# Linda Lab

Empty stage to work on Linda (Lekha's AnimateDiff clips, `../Linda/<clip>/*.png`). Unity 6 (6000.6.7f1), built-in RP, legacy Input.

Double-click `Open in Unity.command` (or open `linda-lab` in Unity Hub), scene `Assets/Linda/Lab.unity`, press Play: A/D walk, J whip, H take a hit. Whip the bag, step in: on the way back it hits her in the back (hit_back); whipping a bag already flying at her ricochets.
Animator `Assets/Linda/Anim/Linda.controller`: idle, walk, hit_whip, hit_back, rotate_left, rotate_right (24 fps).

Pipeline (re-run when Lekha sends new clips):

```sh
/tmp/venv/bin/python Tools/make_linda_frames.py      # ../Linda -> Assets/Linda/Art (needs Pillow numpy scipy)
UNITY=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -quit -projectPath . -executeMethod LindaLab.Build       # or menu Linda/Build Lab
$UNITY -batchmode -quit -projectPath . -executeMethod LindaLabShot.All     # Logs/lab_*.png
```

Play in the browser: https://mrviduus.github.io/linda-lab/

Deploy: `$UNITY -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod LindaLabBuild.WebGL`, then push `Build/WebGL` + `.nojekyll` as branch `gh-pages` (force, with `git -c http.postBuffer=524288000`).
