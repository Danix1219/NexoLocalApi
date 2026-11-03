"""Validate enabled scaffold project references; not a source-code or product audit."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

POLICY = {'Identity': {'Domain': [], 'Application': ['Domain'], 'Infrastructure': ['Application', 'Domain'], 'Api': ['Application', 'Infrastructure']}, 'Directory': {'Features': [], 'Adapters': ['Features'], 'Api': ['Features', 'Adapters']}, 'Community': {'Modules': [], 'Adapters': ['Modules'], 'Api': ['Modules', 'Adapters']}, 'Orders': {'Domain': [], 'Application': ['Domain'], 'Infrastructure': ['Application', 'Domain'], 'Api': ['Application', 'Infrastructure']}, 'Messaging': {'Core': [], 'Adapters': ['Core'], 'Api': ['Core', 'Adapters']}, 'Trust': {'Evaluation': [], 'Adapters': ['Evaluation'], 'Api': ['Evaluation', 'Adapters']}, 'Notifications': {'Delivery': [], 'Adapters': ['Delivery'], 'Api': ['Delivery', 'Adapters'], 'Worker': ['Delivery', 'Adapters']}, 'Relations': {'Projections': [], 'Adapters': ['Projections'], 'Api': ['Projections', 'Adapters']}}
ALLOWED = {"nexolocal.Api": set()}
EXPECTED_PATHS = {"nexolocal.Api": "nexolocal.Api/nexolocal.Api.csproj"}
for service, graph in POLICY.items():
    for project, references in graph.items():
        name = f"NexoLocal.{service}.{project}"
        ALLOWED[name] = {f"NexoLocal.{service}.{ref}" for ref in references}
        EXPECTED_PATHS[name] = f"services/{service.lower()}/src/{project}/{name}.csproj"
CORE_PROJECTS = {name for name, refs in ALLOWED.items() if not refs and name != "nexolocal.Api"} | {"NexoLocal.Identity.Application", "NexoLocal.Orders.Application"}


def validate_graph(graph):
    errors = []
    for name, expected in ALLOWED.items():
        if name not in graph:
            errors.append(f"Missing enabled project: {name}")
        elif graph[name] != expected:
            errors.append(f"Forbidden/missing reference for {name}: expected {sorted(expected)}, got {sorted(graph[name])}")
    for name in graph.keys() - ALLOWED.keys():
        errors.append(f"Unregistered project: {name}; review ownership and reference policy")
    return errors


def check(root):
    graph, errors = {}, []
    projects = list((root / "nexolocal.Api").rglob("*.csproj")) + list((root / "services").rglob("*.csproj"))
    for project in sorted(projects):
        tree = ET.parse(project).getroot()
        if project.stem in graph:
            errors.append(f"Duplicate project name: {project.stem}")
        references = set()
        for reference in tree.findall(".//ProjectReference"):
            target = (project.parent / reference.attrib["Include"].replace("\\", "/")).resolve()
            if not target.is_relative_to(root.resolve()) or not target.is_file():
                errors.append(f"Reference missing/outside code workspace: {project.stem}")
            references.add(target.stem)
        graph[project.stem] = references
        if project.stem in EXPECTED_PATHS and project.relative_to(root).as_posix() != EXPECTED_PATHS[project.stem]:
            errors.append(f"Wrong owner/layer path: {project.stem}")
        if project.stem in CORE_PROJECTS and (tree.findall(".//PackageReference") or tree.findall(".//FrameworkReference")):
            errors.append("Core scaffold has an unreviewed package/framework dependency")
    solution = root / "nexolocal.Api.slnx"
    listed = [(root / p.attrib["Path"]).resolve() for p in ET.parse(solution).getroot().iter("Project")]
    if len(listed) != len(set(listed)) or set(listed) != {p.resolve() for p in projects}:
        errors.append("Solution must include every scaffold project exactly once")
    return errors + validate_graph(graph)


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[1]
    problems = check(root)
    for problem in problems:
        print(f"ARCHITECTURE-FAIL: {problem}")
    if problems:
        sys.exit(1)
    print("ARCHITECTURE-OK: G01, S01-S08 and S07 Worker; 28 projects with per-service reference policies; no product acceptance")
