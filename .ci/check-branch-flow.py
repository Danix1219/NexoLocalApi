"""Validate PR branch routing using GitHub event metadata; no API or secrets."""
import json
import re
import sys


def validate(event):
    pr = event["pull_request"]
    target, source = pr["base"]["ref"], pr["head"]["ref"]
    same_repo = pr["base"]["repo"]["full_name"] == pr["head"]["repo"]["full_name"]
    work = re.fullmatch(r"(?:feature|fix|docs|chore|refactor|test|ci|build|perf|revert)/[A-Za-z0-9][A-Za-z0-9._/-]*", source)
    release = re.fullmatch(r"release/[a-z0-9][a-z0-9._-]*", source)
    if target == "develop":
        allowed = bool(work) or source.startswith("dependabot/") or (same_repo and (source == "main" or bool(release)))
    elif target == "main":
        allowed = same_repo and bool(release)
    elif re.fullmatch(r"release/[a-z0-9][a-z0-9._-]*", target):
        allowed = same_repo and bool(re.fullmatch(r"fix/[A-Za-z0-9][A-Za-z0-9._/-]*", source))
    else:
        allowed = False
    if not allowed:
        raise ValueError(f"PR route forbidden: {source} -> {target}. Follow docs/flujo-git.md")
    return f"FLOW-OK: {source} -> {target} (metadata only; no product acceptance)"


if __name__ == "__main__":
    with open(sys.argv[1], encoding="utf-8") as stream:
        print(validate(json.load(stream)))
