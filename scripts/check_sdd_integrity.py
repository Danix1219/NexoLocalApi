"""Check backend documentary traceability and local task cycles, without product claims."""
from pathlib import Path
import re
import sys


def inspect(folder):
    files = {name: (folder / name).read_text(encoding="utf-8-sig")
             for name in ("spec.md", "plan.md", "task.md", "checklists/gates.md")}
    spec, tasks = files["spec.md"], files["task.md"]
    errors = []
    definitions = {
        "requirements": re.findall(r"^- ((?:FR|NFR)-\d{3}):", spec, re.M),
        "criteria": re.findall(r"^- \[ \] (AC-\d{3})\b", spec, re.M),
        "tasks": re.findall(r"^- \[ \] (T-\d{3})\b", tasks, re.M),
    }
    for kind, values in definitions.items():
        if len(values) != len(set(values)):
            errors.append(f"duplicate {kind}")
    defined = set(sum(definitions.values(), []))
    for reference in set(re.findall(r"\b(?:FR|NFR|AC|T)-\d{3}\b", "\n".join(files.values()))):
        if reference not in defined:
            errors.append(f"undefined reference {reference}")
    for requirement in definitions["requirements"]:
        if not re.search(rf"\b{requirement}\b", tasks) or not re.search(rf"^\| {requirement} \|", spec, re.M):
            errors.append(f"requirement missing task/matrix: {requirement}")
    for criterion in definitions["criteria"]:
        if not re.search(rf"\b{criterion}\b", tasks):
            errors.append(f"criterion missing task: {criterion}")
    graph = {}
    for match in re.finditer(r"^- \[ \] (T-\d{3}) (.*)$", tasks, re.M):
        task_id, line = match.groups()
        if "Acepta:" not in line or not re.search(r"\b(?:FR|NFR)-\d{3}\b", line):
            errors.append(f"task missing requirement/acceptance: {task_id}")
        dependency = line.split("Depende de:", 1)[1] if "Depende de:" in line else ""
        graph[task_id] = set(re.findall(r"\bT-\d{3}\b", dependency))
    active, visited = set(), set()

    def walk(task_id):
        if task_id in active:
            errors.append(f"task cycle at {task_id}")
            return
        if task_id in visited:
            return
        active.add(task_id)
        for dependency in graph.get(task_id, set()):
            walk(dependency)
        active.remove(task_id)
        visited.add(task_id)

    for task_id in graph:
        walk(task_id)
    if "FR-950" in defined and "T-950" not in graph.get("T-001", set()):
        errors.append("Ready task T-001 must depend on structural preparation T-950")
    if re.search(r"^- \[[xX]\]", tasks, re.M):
        errors.append("executed task in documentary phase; review status explicitly")
    return errors, {key: len(value) for key, value in definitions.items()}


def main(root):
    folders = sorted(path for path in (root / "specs").glob("*/*")
                     if path.is_dir() and re.fullmatch(r"(?:nv|ep)-\d{2}", path.name))
    if not folders:
        print("SDD-INTEGRITY-FAIL: no backend specs")
        return 1
    failures, total = [], dict(tasks=0, requirements=0, criteria=0)
    for folder in folders:
        errors, counts = inspect(folder)
        failures.extend(f"{folder.name}: {error}" for error in errors)
        for kind, count in counts.items():
            total[kind] += count
    for failure in failures:
        print(f"SDD-INTEGRITY-FAIL: {failure}")
    if failures:
        return 1
    print(f"SDD-INTEGRITY-OK: {len(folders)} specs; {total}; local task graph only, no product acceptance")
    return 0


if __name__ == "__main__":
    sys.exit(main(Path(__file__).resolve().parents[1]))
