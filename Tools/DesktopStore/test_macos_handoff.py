"""Focused checks for the Mac signing handoff; uses only synthetic credentials."""
from __future__ import annotations

import io
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch

import run_macos_ci as handoff


APP = "Speed Rovers 2.app"


class MacHandoffTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="speedrovers-handoff-test-")
        self.root = Path(self.temporary.name)
        self.destination = self.root / "destination"
        self.destination.mkdir()
        self.archive = self.root / "fixture.tar.gz"

    def tearDown(self):
        self.temporary.cleanup()

    def write_archive(self, *entries):
        with tarfile.open(self.archive, "w:gz") as archive:
            for name, kind in entries:
                member = tarfile.TarInfo(name)
                member.type = kind
                if kind in (tarfile.SYMTYPE, tarfile.LNKTYPE):
                    member.linkname = "../../outside"
                payload = b"test-only player data" if kind == tarfile.REGTYPE else b""
                member.size = len(payload)
                archive.addfile(member, io.BytesIO(payload) if payload else None)

    def rejected_without_writes(self, *entries):
        # Put a benign file first: validation must finish before any extraction.
        self.write_archive((APP + "/Contents/valid", tarfile.REGTYPE), *entries)
        with self.assertRaises(ValueError):
            handoff.extract_player_archive(self.archive, self.destination, APP)
        self.assertEqual(list(self.destination.iterdir()), [])
        self.assertFalse((self.root / "outside").exists())

    def test_benign_archive_extracts(self):
        self.write_archive((APP, tarfile.DIRTYPE),
                           (APP + "/Contents/data", tarfile.REGTYPE))
        handoff.extract_player_archive(self.archive, self.destination, APP)
        self.assertEqual((self.destination / APP / "Contents/data").read_bytes(),
                         b"test-only player data")

    def test_rejects_traversal(self):
        for name in (APP + "/../outside", APP + "/Contents/../../outside",
                     APP + "/./data", APP + "//data"):
            with self.subTest(name=name):
                self.rejected_without_writes((name, tarfile.REGTYPE))

    def test_rejects_absolute_and_foreign_paths(self):
        for name in ("/outside", "Other.app/data", "C:/outside",
                     APP + "\\outside", APP + "/data\nline"):
            with self.subTest(name=name):
                self.rejected_without_writes((name, tarfile.REGTYPE))

    def test_rejects_symlink(self):
        self.rejected_without_writes((APP + "/Contents/link", tarfile.SYMTYPE))

    def test_rejects_hardlink(self):
        self.rejected_without_writes((APP + "/Contents/link", tarfile.LNKTYPE))

    def test_rejects_duplicate(self):
        self.rejected_without_writes((APP + "/Contents/valid", tarfile.REGTYPE))

    def test_rejects_case_collision(self):
        self.rejected_without_writes((APP + "/Contents/VALID", tarfile.REGTYPE))

    def test_rejects_special_files(self):
        for kind in (tarfile.FIFOTYPE, tarfile.CHRTYPE, tarfile.BLKTYPE):
            with self.subTest(kind=kind):
                self.rejected_without_writes((APP + "/Contents/special", kind))

    def test_rejects_empty_archive(self):
        self.write_archive()
        with self.assertRaises(ValueError):
            handoff.extract_player_archive(self.archive, self.destination, APP)

    def test_rejects_nonempty_destination(self):
        self.write_archive((APP + "/Contents/data", tarfile.REGTYPE))
        original = self.destination / "existing"
        original.write_text("keep", encoding="utf-8")
        with self.assertRaises(ValueError):
            handoff.extract_player_archive(self.archive, self.destination, APP)
        self.assertEqual(list(self.destination.iterdir()), [original])
        self.assertEqual(original.read_text(encoding="utf-8"), "keep")

    def test_password_is_environment_reference_only(self):
        variable = "SPEEDROVERS_TEST_CERTIFICATE_PASSWORD"
        dummy = "synthetic-test-secret-DO-NOT-PASS-IN-ARGV"
        with patch.dict(os.environ, {variable: dummy}, clear=True), \
                patch.object(handoff, "keychain") as keychain:
            handoff.import_certificate(Path("test-only.p12"), variable)
        arguments = keychain.call_args.args
        self.assertIn("@env:" + variable, arguments)
        self.assertNotIn(dummy, arguments)
        self.assertIn("--certificate-password", arguments)

    def test_no_password_option_when_unset(self):
        with patch.dict(os.environ, {}, clear=True), \
                patch.object(handoff, "keychain") as keychain:
            handoff.import_certificate(Path("test-only.p12"), "UNSET_TEST_PASSWORD")
        self.assertNotIn("--certificate-password", keychain.call_args.args)

    def test_old_python_is_rejected_before_credentials(self):
        arguments = ["run_macos_ci.py", "--archive", "unused.tar.gz",
                     "--inspection-report", "unused.json", "--output-dir", "unused"]
        with patch.object(handoff.sys, "argv", arguments), \
                patch.object(handoff.sys, "version_info", (3, 12, 10)), \
                patch.object(handoff.platform, "system", return_value="Darwin"), \
                patch.object(handoff, "required") as required:
            with self.assertRaisesRegex(SystemExit, "3.12.11"):
                handoff.main()
        required.assert_not_called()


if __name__ == "__main__":
    unittest.main()
