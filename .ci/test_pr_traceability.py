"""Synthetic .test fixtures only; no real Jira issues/pages or network calls."""
import copy
import importlib.util
from pathlib import Path
import unittest

module_spec = importlib.util.spec_from_file_location("traceability", Path(__file__).with_name("check-pr-traceability.py"))
policy_check = importlib.util.module_from_spec(module_spec)
module_spec.loader.exec_module(policy_check)


class TraceabilityTests(unittest.TestCase):
    def setUp(self):
        self.policy = {"jira_project_keys": ["TEST"], "jira_base_url": "https://jira.test", "confluence_base_url": "https://confluence.test/wiki"}
        self.event = {"pull_request": {
            "title": "feat(cuenta): TEST-123 recuperar cuenta",
            "body": "Jira principal: TEST-123\nJira: https://jira.test/browse/TEST-123\nConfluence: https://confluence.test/wiki/spaces/TEST/pages/123/Recovery\n",
            "head": {"ref": "feature/TEST-123-nv-16-recuperacion", "repo": {"full_name": "team/api"}},
            "base": {"ref": "develop", "repo": {"full_name": "team/api"}},
            "user": {"login": "contributor"},
        }}
        self.pr = self.event["pull_request"]

    def check(self):
        return policy_check.validate(self.event, self.policy)

    def rejects(self):
        with self.assertRaises(ValueError):
            self.check()

    def test_valid_work(self): self.check()
    def test_missing_policy(self): self.policy["jira_project_keys"] = []; self.rejects()
    def test_missing_site(self): self.policy["confluence_base_url"] = None; self.rejects()
    def test_missing_body(self): self.pr["body"] = None; self.rejects()
    def test_missing_confluence(self): self.pr["body"] = self.pr["body"].split("Confluence:")[0]; self.rejects()
    def test_empty_template_field(self): self.pr["body"] = self.pr["body"].split("Confluence:")[0] + "Confluence:\n"; self.rejects()
    def test_link_only_in_comment(self): self.pr["body"] = "<!--" + self.pr["body"] + "-->"; self.rejects()
    def test_link_only_in_backtick_fence(self): self.pr["body"] = "```text\n" + self.pr["body"] + "```"; self.rejects()
    def test_link_only_in_tilde_fence(self): self.pr["body"] = "~~~\n" + self.pr["body"] + "~~~"; self.rejects()
    def test_link_only_in_quote(self): self.pr["body"] = "> " + self.pr["body"].replace("\n", "\n> "); self.rejects()
    def test_duplicate_field(self): self.pr["body"] += "Confluence: https://confluence.test/wiki/x/abc\n"; self.rejects()
    def test_wrong_confluence_host(self): self.pr["body"] = self.pr["body"].replace("confluence.test", "elsewhere.test"); self.rejects()
    def test_host_suffix_attack(self): self.pr["body"] = self.pr["body"].replace("confluence.test", "confluence.test.other.test"); self.rejects()
    def test_credentials_in_url(self): self.pr["body"] = self.pr["body"].replace("https://confluence.test", "https://user:secret@confluence.test"); self.rejects()
    def test_http_rejected(self): self.pr["body"] = self.pr["body"].replace("https://confluence.test", "http://confluence.test"); self.rejects()
    def test_space_home_rejected(self): self.pr["body"] = self.pr["body"].replace("/pages/123/Recovery", ""); self.rejects()
    def test_wrong_title_key(self): self.pr["title"] = self.pr["title"].replace("TEST-123", "TEST-124"); self.rejects()
    def test_nonconventional_title(self): self.pr["title"] = "TEST-123 cambio"; self.rejects()
    def test_wrong_branch_key(self): self.pr["head"]["ref"] = "feature/TEST-124-cuenta"; self.rejects()
    def test_missing_branch_key(self): self.pr["head"]["ref"] = "feature/nv-16-cuenta"; self.rejects()
    def test_unapproved_project(self): self.policy["jira_project_keys"] = ["OTHER"]; self.rejects()
    def test_wrong_jira_target(self): self.pr["body"] = self.pr["body"].replace("/browse/TEST-123", "/browse/TEST-124"); self.rejects()
    def test_browse_placeholder(self): self.pr["body"] = self.pr["body"].replace("/browse/TEST-123", "/browse/<CLAVE>"); self.rejects()
    def test_release_to_main(self): self.pr["head"]["ref"] = "release/0.1.0-rc.1"; self.pr["base"]["ref"] = "main"; self.check()
    def test_main_reintegration(self): self.pr["head"]["ref"] = "main"; self.check()
    def test_fix_to_release(self): self.pr["head"]["ref"] = "fix/TEST-123-error"; self.pr["base"]["ref"] = "release/0.1.0"; self.check()
    def test_feature_to_release_forbidden(self): self.pr["base"]["ref"] = "release/0.1.0"; self.rejects()
    def test_feature_to_main_forbidden(self): self.pr["base"]["ref"] = "main"; self.rejects()
    def test_develop_to_main_forbidden(self): self.pr["head"]["ref"] = "develop"; self.pr["base"]["ref"] = "main"; self.rejects()
    def test_fork_work_allowed(self): self.pr["head"]["repo"]["full_name"] = "fork/api"; self.check()
    def test_fork_promotion_forbidden(self): self.pr["head"]["repo"]["full_name"] = "fork/api"; self.pr["head"]["ref"] = "release/0.1.0"; self.pr["base"]["ref"] = "main"; self.rejects()
    def test_dependabot_requires_metadata(self): self.pr["head"]["ref"] = "dependabot/github_actions/action-1"; self.pr["user"]["login"] = "dependabot[bot]"; self.check(); self.pr["body"] = ""; self.rejects()
    def test_fake_dependabot_branch_forbidden(self): self.pr["head"]["ref"] = "dependabot/fake"; self.rejects()
    def test_permalink(self): self.pr["body"] = self.pr["body"].replace("spaces/TEST/pages/123/Recovery", "pages/viewpage.action?pageId=123"); self.check()
    def test_shortlink(self): self.pr["body"] = self.pr["body"].replace("spaces/TEST/pages/123/Recovery", "x/AbCd"); self.check()
    def test_legacy_display_link(self): self.pr["body"] = self.pr["body"].replace("spaces/TEST/pages/123/Recovery", "display/TEST/Recovery"); self.check()
    def test_encoded_traversal(self): self.pr["body"] = self.pr["body"].replace("/wiki/spaces", "/wiki/%2e%2e/spaces"); self.rejects()
    def test_title_shell_text_is_data(self): self.pr["title"] += " $(echo secret)"; self.check()
    def test_comment_example_does_not_count(self): self.pr["body"] += "<!--\nConfluence: https://bad.test/wiki/x/example\n-->"; self.check()


if __name__ == "__main__":
    unittest.main()
