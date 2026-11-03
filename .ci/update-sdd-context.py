"""Refresh paired documentary fixtures. Never copy Git history, code or credentials."""
from pathlib import Path
import hashlib
import io
import json
import zipfile

workspace = Path(__file__).resolve().parents[2]
repositories = ("API_NexoLocal", "App_NexoLocal")
shared = workspace / "docs_nexo"
for name in repositories:
    root = workspace / name
    manifest_path = root / ".ci/context-manifest.json"
    old = json.loads(manifest_path.read_text(encoding="utf-8"))
    paths = {workspace / item["path"] for item in old["files"]}
    # Narrow refresh: only documentary references already exported by the tracked manifest.
    # Do not discover new PDFs, workbooks or whole counterpart trees.
    if any(p.suffix.lower() not in {".md", ".json"} for p in paths):
        raise ValueError("Review non-documentary references explicitly before exporting them")
    # Removed historical references must not revive a retired tool directory.
    paths = {p for p in paths if p.is_file() and ".opencode" not in p.parts}
    output = io.BytesIO()
    files = []
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(paths):
            relative = path.relative_to(workspace).as_posix()
            if relative.split("/")[0] not in old["allowedRoots"]:
                raise ValueError(f"Unexpected fixture root: {relative}")
            content = path.read_bytes()
            info = zipfile.ZipInfo(relative, date_time=(2026, 11, 3, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            archive.writestr(info, content)
            files.append({"path": relative, "sha256": hashlib.sha256(content).hexdigest()})
    content = output.getvalue()
    (root / ".ci/sdd-context.zip").write_bytes(content)
    old.update({"revision": "SDD-publicacion-2026-11-03", "archiveSha256": hashlib.sha256(content).hexdigest(), "files": files})
    old["snapshotNote"] = "Refresh of existing public documentary references only; no newly exported PDFs/workbooks or bulk counterpart copy."
    manifest_path.write_text(json.dumps(old, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"CONTEXT-UPDATED: {name}: {len(files)} referenced/source files; {len(content)} bytes")
