# my-first-feature (Onshape FeatureScript)

## Purpose
A minimal, working FeatureScript project used to develop and test custom features in Onshape while keeping **GitHub as the source of truth**.

## Source of truth
- **Authoring / review:** VS Code + this repository (diffs + commits)
- **Runtime:** Onshape Feature Studio (paste/import from this repo)

Rule: **Do not make edits directly in Onshape** except for quick experiments that you immediately port back into the repo.

## Files
- `src/box-test.fs` — FeatureScript source
- `notes/` — optional design notes, decision log, bug notes (plain `.md` files)

## How to run in Onshape
1. Create/open an Onshape Document.
2. Create a **Feature Studio**.
3. Paste the full contents of `src/box-test.fs` into the Feature Studio and **Save**.
4. In a **Part Studio**, add the custom feature from the Feature Studio.
5. Run the feature and verify geometry updates as expected.

Tip: after you paste into Onshape, update the header comment in `box-test.fs` with the short git commit hash you pasted.

## Development workflow
### Make a change
1. Edit in VS Code.
2. Open the diff (**Source Control → right-click file → Open Changes**).
3. Commit with a message that explains intent.
4. Sync/push to GitHub.
5. Paste the full file into Onshape and test.

### Minimum commit hygiene
- Keep commits small (one behavior change per commit).
- Use present-tense messages (e.g., “Add centered option”).
- If a commit fixes a bug, include the Onshape error text in the commit message body.

## How to collaborate with ChatGPT
ChatGPT is most reliable when it produces **mechanical edits** against the exact current code.

When asking for help, include:
- The **exact** current function or file text.
- The goal/acceptance criteria.
- Any Onshape error text.

Preferred request formats:
- **“Provide a unified diff that changes X to Y.”**
- **“Replace this entire function with a corrected version.”**

Avoid:
- “Make it better” without specifying expected behavior.
- Pasting partial snippets when the bug may be elsewhere.

## References
Primary documentation for this project:
- FeatureScript docs: https://cad.onshape.com/FsDoc/index.html
- Onshape forum (secondary): https://forum.onshape.com/
- Std library mirror (secondary): https://github.com/javawizard/onshape-std-library-mirror

## Troubleshooting
- If Onshape reports a precondition/UI error, it’s often because a parameter type isn’t supported by the feature dialog. Prefer `isLength`, `isAngle`, `isInteger`, `isString`, `isQuery`, and `definition.foo is boolean`.
- If Onshape reports “Function X not found,” verify imports and that you’re calling the correct std function (some are `fSomething` features vs `opSomething` operations).
