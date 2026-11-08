"""PR traceability policy. Reads local JSON only; never fetches Jira/Confluence."""
import json
import re
import sys
from urllib.parse import parse_qs, unquote, urlsplit

ISSUE = r"[A-Z][A-Z0-9_]*-[1-9][0-9]*"
WORK = re.compile(rf"(?:feature|fix|docs|chore|refactor|test|ci|build|perf|revert)/(?P<issue>{ISSUE})-[a-z0-9]+(?:-[a-z0-9]+)*")
RELEASE = re.compile(r"release/(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[a-z0-9]+(?:[.-][a-z0-9]+)*)?")
TITLE = re.compile(rf"(?:feat|fix|docs|chore|refactor|test|ci|build|perf|revert|release)(?:\([a-z0-9-]+\))?!?: (?P<issue>{ISSUE}) \S.+")


def clean_body(body):
    text = re.sub(r"<!--.*?-->", "", body or "", flags=re.S)
    lines, fence = [], None
    for line in text.splitlines():
        opening = re.match(r"^\s{0,3}(`{3,}|~{3,})", line)
        if opening:
            marker = opening.group(1)
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
            continue
        if fence is None and not re.match(r"^\s*(>| {4}|\t)", line):
            lines.append(line)
    return "\n".join(lines)


def field(text, name):
    matches = re.findall(rf"^{re.escape(name)}:[ \t]*(.*)$", text, flags=re.M)
    if len(matches) != 1 or not matches[0].strip():
        raise ValueError(f"Required exactly once: {name}: <value>")
    return matches[0].strip()


def url(value):
    if re.search(r"[\s<>`\\]", value):
        raise ValueError("Use a plain absolute HTTPS URL, not a placeholder or Markdown link")
    parsed = urlsplit(value)
    if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password:
        raise ValueError("HTTPS URL without embedded credentials required")
    # Reading port also rejects malformed ports. No network request is made.
    parsed.port
    return parsed


def relative_path(value, base):
    parsed, root = url(value), url(base)
    path, prefix = unquote(parsed.path), unquote(root.path).rstrip("/")
    if parsed.netloc.lower() != root.netloc.lower() or not path.startswith(prefix + "/"):
        raise ValueError("URL does not belong to the configured Jira/Confluence base")
    if any(part in (".", "..") for part in path.split("/")):
        raise ValueError("URL contains path traversal")
    return parsed, path[len(prefix):]


def validate(event, policy):
    keys = policy.get("jira_project_keys", [])
    if not keys or not policy.get("jira_base_url") or not policy.get("confluence_base_url"):
        raise ValueError("Configure real Jira project keys, jira_base_url and confluence_base_url before activation")
    if any(not re.fullmatch(r"[A-Z][A-Z0-9_]*", key) for key in keys):
        raise ValueError("Invalid Jira project key in policy")
    pr = event["pull_request"]
    body = clean_body(pr.get("body"))
    primary = field(body, "Jira principal")
    jira_url = field(body, "Jira")
    confluence_url = field(body, "Confluence")
    if not re.fullmatch(ISSUE, primary) or primary.rsplit("-", 1)[0] not in keys:
        raise ValueError("Jira principal must be a key from a configured project")
    title = TITLE.fullmatch(pr.get("title", ""))
    if title is None or title.group("issue") != primary:
        raise ValueError("Title must be tipo(ambito): CLAVE-123 resumen, matching Jira principal")
    parsed, path = relative_path(jira_url, policy["jira_base_url"])
    if path != "/browse/" + primary or parsed.query or parsed.fragment:
        raise ValueError("Jira must link directly to /browse/<Jira principal>")
    parsed, path = relative_path(confluence_url, policy["confluence_base_url"])
    page = re.fullmatch(r"/spaces/[^/]+/pages/[1-9][0-9]*(?:/[^/]+)?/?", path)
    legacy = re.fullmatch(r"/display/[^/]+/[^/]+/?", path)
    short = re.fullmatch(r"/x/[A-Za-z0-9_-]+/?", path)
    page_ids = parse_qs(parsed.query).get("pageId", [])
    permalink = path == "/pages/viewpage.action" and len(page_ids) == 1 and re.fullmatch(r"[1-9][0-9]*", page_ids[0])
    if not (page or legacy or short or permalink):
        raise ValueError("Confluence must point to a page/permalink, not the site or space home")
    source, target = pr["head"]["ref"], pr["base"]["ref"]
    same_repo = pr["head"]["repo"]["full_name"] == pr["base"]["repo"]["full_name"]
    work = WORK.fullmatch(source)
    release = RELEASE.fullmatch(source)
    bot = source.startswith("dependabot/") and pr.get("user", {}).get("login") == "dependabot[bot]"
    if work and work.group("issue") != primary:
        raise ValueError("Branch Jira key must match Jira principal and PR title")
    allowed = False
    if target == "develop":
        allowed = bool(work) or bot or (same_repo and (source == "main" or bool(release)))
    elif target == "main":
        allowed = same_repo and bool(release)
    elif RELEASE.fullmatch(target):
        allowed = same_repo and bool(work) and source.startswith("fix/")
    if not allowed:
        raise ValueError("Branch name or PR route does not follow docs/flujo-git.md")
    return "TRACEABILITY-OK: Jira key/title/branch and required documentation URLs validated structurally; no remote acceptance verified"


if __name__ == "__main__":
    with open(sys.argv[1], encoding="utf-8") as stream:
        event = json.load(stream)
    with open(sys.argv[2], encoding="utf-8") as stream:
        policy = json.load(stream)
    # Jira y Confluence todavia no existen para este proyecto. Mientras la politica este vacia la
    # comprobacion no bloquea: exigir una clave de un Jira inexistente no trazaria nada, solo
    # obligaria a inventarla. Se activa sola al llenar `.github/change-policy.json`, y entonces
    # `validate` vuelve a rechazar lo que no cumpla, incluida la politica a medio configurar.
    if not policy.get("jira_project_keys") or not policy.get("jira_base_url") or not policy.get("confluence_base_url"):
        print("TRACEABILITY-SKIPPED: sin Jira ni Confluence configurados en .github/change-policy.json; "
              "la trazabilidad del PR no se comprueba todavia")
        sys.exit(0)
    try:
        print(validate(event, policy))
    except (ValueError, KeyError, TypeError) as error:
        print(f"PR traceability failed: {error}", file=sys.stderr)
        sys.exit(1)
