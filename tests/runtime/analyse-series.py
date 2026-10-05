"""Evaluate complete attempts separately; never pool phases across attempts."""
import json, re, subprocess, sys, tempfile
from pathlib import Path

source = Path(sys.argv[1])
log = source.read_text(encoding="utf-8-sig")
starts = list(re.finditer(r"^.*TRIAL baseline-walk .*?$", log, re.M))
reports = []
with tempfile.TemporaryDirectory() as folder:
    for index, start in enumerate(starts):
        end = starts[index + 1].start() if index + 1 < len(starts) else len(log)
        attempt = log[start.start():end]
        path = Path(folder) / f"attempt-{index+1}.log"
        out = path.with_suffix(".json")
        path.write_text(attempt, encoding="utf-8")
        subprocess.run([sys.executable, str(Path(__file__).with_name("analyse-runtime.py")), str(path), str(out)], capture_output=True, check=False)
        report = json.loads(out.read_text())
        report["attempt"] = index + 1
        report["start"] = start.group().strip()
        contamination = []
        if "Gamepad device" in attempt:
            contamination.append("Unexpected ability-button input during scripted attempt")
        for action in ("walk", "jump"):
            for mode in ("baseline", "active"):
                r = report["results"].get(f"{mode}-{action}", {})
                if r.get("punchCalls", 0) != 0:
                    contamination.append(f"Unscripted punch during {mode}-{action}")
        report["invalid_reasons"] = contamination
        report["valid"] = report["checks"]["all_six_trials"] and report["checks"]["trial_completed_without_abort"] and not contamination
        report["passed"] = report["passed"] and report["valid"]
        reports.append(report)
result = {"attempts": reports, "valid_attempts": sum(r["valid"] for r in reports),
          "passing_attempts": sum(r["passed"] for r in reports),
          "repeatability_passed": len(reports) >= 3 and all(r["passed"] for r in reports),
          "limits": "Each attempt retains the original eight acceptance checks. Incomplete, aborted and contaminated attempts remain visible and cannot count as passes."}
text = json.dumps(result, indent=2)
if len(sys.argv) > 2:
    Path(sys.argv[2]).write_text(text, encoding="utf-8")
print(text)
sys.exit(0 if result["repeatability_passed"] else 1)
