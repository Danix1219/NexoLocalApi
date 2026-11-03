"""Negative fixtures for SDD dependency/coverage rules; no product tests."""
from pathlib import Path
import unittest
from unittest.mock import patch

import check_sdd_integrity as checker


class IntegrityTests(unittest.TestCase):
    def setUp(self):
        self.folder = Path("synthetic-sdd-fixture")
        self.spec = (
            "- FR-001: sample requirement\n- NFR-950: preparation safety\n"
            "- FR-950: structural preparation\n"
            "- [ ] AC-001: evidence\n- [ ] AC-950: prepared\n"
            "| FR-001 | T-001 | AC-001 |\n"
            "| FR-950 | T-950 | AC-950 |\n"
            "| NFR-950 | T-950 | AC-950 |\n"
        )
        self.tasks = (
            "- [ ] T-001 →FR-001. Ready. Acepta: AC-001. Depende de: T-950.\n"
            "- [ ] T-950 →FR-950,NFR-950. Prepare. Acepta: AC-950. Depende de: ninguna.\n"
        )

    def run_check(self):
        values = {"spec.md": self.spec, "task.md": self.tasks,
                  "plan.md": "Preparation before Ready.\n",
                  "gates.md": "Evidence pending.\n"}
        with patch.object(Path, "read_text", lambda path, **kwargs: values[path.name]):
            return checker.inspect(self.folder)[0]

    def test_valid_graph(self):
        self.assertEqual([], self.run_check())

    def test_cycle(self):
        self.tasks = self.tasks.replace("Depende de: ninguna", "Depende de: T-001")
        self.assertTrue(any("cycle" in error for error in self.run_check()))

    def test_undefined_task(self):
        self.tasks = self.tasks.replace("Depende de: ninguna", "Depende de: T-999")
        self.assertTrue(any("undefined" in error for error in self.run_check()))

    def test_duplicate_requirement(self):
        self.spec += "- FR-001: duplicate\n"
        self.assertTrue(any("duplicate" in error for error in self.run_check()))

    def test_missing_matrix(self):
        self.spec = self.spec.replace("| FR-001 | T-001 | AC-001 |\n", "")
        self.assertTrue(any("missing task/matrix" in error for error in self.run_check()))

    def test_missing_criterion_task(self):
        self.spec += "- [ ] AC-999: orphan\n"
        self.assertTrue(any("criterion missing task" in error for error in self.run_check()))

    def test_ready_without_preparation(self):
        self.tasks = self.tasks.replace("Depende de: T-950", "Depende de: ninguna")
        self.assertTrue(any("Ready task" in error for error in self.run_check()))

    def test_task_marked_executed(self):
        self.tasks = self.tasks.replace("[ ] T-001", "[x] T-001")
        self.assertTrue(any("executed task" in error for error in self.run_check()))


if __name__ == "__main__":
    unittest.main()
