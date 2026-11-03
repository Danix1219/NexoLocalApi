"""Regression tests of dependency rules, using synthetic graphs only."""
from pathlib import Path
import copy
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
from check_architecture import ALLOWED, validate_graph


class ProjectReferencesTests(unittest.TestCase):
    def setUp(self):
        self.graph = copy.deepcopy(ALLOWED)

    def test_allowed_graph(self):
        self.assertEqual([], validate_graph(self.graph))

    def test_domain_cannot_reference_infrastructure(self):
        self.graph["NexoLocal.Identity.Domain"].add("NexoLocal.Identity.Infrastructure")
        self.assertTrue(validate_graph(self.graph))

    def test_application_cannot_reference_api(self):
        self.graph["NexoLocal.Identity.Application"].add("NexoLocal.Identity.Api")
        self.assertTrue(validate_graph(self.graph))

    def test_cross_service_reference_rejected(self):
        self.graph["NexoLocal.Identity.Application"].add("NexoLocal.Orders.Domain")
        self.assertTrue(validate_graph(self.graph))

    def test_gateway_cannot_reference_service_project(self):
        self.graph["nexolocal.Api"].add("NexoLocal.Identity.Application")
        self.assertTrue(validate_graph(self.graph))

    def test_missing_project_rejected(self):
        del self.graph["NexoLocal.Identity.Domain"]
        self.assertTrue(validate_graph(self.graph))

    def test_new_service_requires_reviewed_policy(self):
        self.graph["NexoLocal.Unknown.Domain"] = set()
        self.assertTrue(validate_graph(self.graph))

    def test_missing_required_reference_rejected(self):
        self.graph["NexoLocal.Identity.Api"].clear()
        self.assertTrue(validate_graph(self.graph))

    def test_worker_cannot_reference_api(self):
        self.graph["NexoLocal.Notifications.Worker"].add("NexoLocal.Notifications.Api")
        self.assertTrue(validate_graph(self.graph))

    def test_projection_has_no_artificial_domain(self):
        self.graph["NexoLocal.Relations.Projections"].add("NexoLocal.Relations.Domain")
        self.assertTrue(validate_graph(self.graph))

    def test_existing_services_cannot_reference_each_other(self):
        self.graph["NexoLocal.Orders.Infrastructure"].add("NexoLocal.Directory.Features")
        self.assertTrue(validate_graph(self.graph))

    def test_directory_feature_cannot_depend_on_adapter(self):
        self.graph["NexoLocal.Directory.Features"].add("NexoLocal.Directory.Adapters")
        self.assertTrue(validate_graph(self.graph))

    def test_evaluation_cannot_depend_on_provider(self):
        self.graph["NexoLocal.Trust.Evaluation"].add("NexoLocal.Trust.Adapters")
        self.assertTrue(validate_graph(self.graph))

    def test_delivery_cannot_depend_on_worker(self):
        self.graph["NexoLocal.Notifications.Delivery"].add("NexoLocal.Notifications.Worker")
        self.assertTrue(validate_graph(self.graph))


if __name__ == "__main__":
    unittest.main()
