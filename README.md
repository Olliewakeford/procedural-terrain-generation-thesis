# Procedural Terrain Generation with Fixed Points

Bachelor's thesis, Charles University (Faculty of Mathematics and Physics), Prague.
Defended 5 September 2025. Author: Oliver Wakeford. Supervisor: Petr Šimůnek.

Official record (Charles University Digital Repository, permanent handle):
https://dspace.cuni.cz/handle/20.500.11956/202622

This repository is a consolidated, portfolio-ready copy of the thesis and its
accompanying Unity implementation. It replaces four scattered repos with one
place to read the thesis, see the code, and understand what the results
actually show.

## The problem

Procedural terrain generation (Perlin noise, Voronoi diagrams, midpoint
displacement, erosion simulation, etc.) produces natural-looking landscapes,
but game design often needs some parts of the terrain to be exact
(a fixed spawn plateau, a hand-placed valley, a building foundation) while
the rest fills in procedurally around it. Naively generating around fixed
points creates a visible seam where the fixed and generated terrain meet.

The thesis addresses this: given a terrain and a mask marking which cells are
fixed and which are free, generate natural-looking terrain in the free cells
that blends smoothly into the fixed ones, without requiring the fixed
regions to look artificial.

## The method

Implemented as a Unity 6 component system (`/unity`):

- **Generators** (Perlin/fBM noise, Voronoi, midpoint displacement/diamond-square,
  uniform height) fill the modifiable region.
- **Modifiers** (basic smoothing, distance-weighted smoothing, distance-based
  height scaling, hydraulic erosion, thermal erosion) then blend the
  generated terrain into the fixed points. The key idea is a distance grid:
  every modifiable cell knows its distance to the nearest fixed cell, and
  smoothing/scaling strength is weighted by that distance: strong blending
  near the seam, full-strength generation far from it.
- **Interactive Genetic Algorithm (IGA)**: rather than hand-tuning generator
  and modifier parameters, the tool can evolve them. It generates a
  population of terrain variants, the user visually selects favourites, and
  the algorithm breeds/mutates parameters over generations until the user is
  happy with a result. This replaces manual parameter search with
  preference-guided search.
- Long operations (smoothing, erosion) are GPU-accelerated via compute
  shaders with CPU fallbacks.

Full architecture and extension notes: `unity/DEVELOPER_DOCUMENTATION.md`.
Usage instructions (installing into a Unity project, setting up a constrained
terrain, running the IGA, per-parameter reference): `unity/README.md`.

## What the thesis found

The distance-weighted blending approach produces visibly smoother transitions
between fixed and generated terrain than a naive edge blend, and the IGA
lets a user reach an acceptable terrain without manually tuning ~15
generator/modifier parameters. The official thesis review (Charles
University opponent report) rated the approach above the typical standard
for a bachelor's thesis, specifically for combining the two parts (blending
+ genetic-algorithm parameter search) rather than either alone.

## The follow-on paper, and how the credit splits

This thesis became the basis for a peer-reviewed paper:

**"Constraint-Aware Terrain Completion from Sparse Fixed Regions for Game
Heightmaps"**, accepted for oral presentation and full-paper publication at
**IEEE Conference on Games (CoG) 2026**, Madrid, 1–4 September 2026
(89 of 239 submissions accepted, 37.2% acceptance rate).

**I am second author.** My supervisor, Petr Šimůnek, took the thesis
method, extended it, ran the experiments reported in the paper, and led the
writing. The thesis established the approach and the initial implementation;
the paper's benchmark results, and the writing that presents them, are
Petr's work built on that foundation. It is not my result to claim as
primary author, and this README does not present it as one.

No public link to the paper exists yet, because CoG 2026 hasn't happened. This
section will be updated with a DOI/proceedings link once published.

### Paper results, including the parts that don't look good

Because the honest picture matters more than the flattering one:

- **Mean-height bias**: the method achieves a normalised mean-height bias of
  0.01346, versus 0.07112 for a blurred-production baseline, roughly an
  81% reduction.
- **Speed**: 23.9x to 2.1x faster than a harmonic-interpolation baseline,
  depending on resolution (the speedup shrinks as resolution grows).
- **Against IDW (inverse distance weighting)**: the method's mean-height
  bias (0.01351) is only 0.4% better than IDW's. On this metric it barely
  beats a much simpler baseline.
- **Total variation**: meaningfully *worse* than the harmonic baseline,
  933.5 vs 480.9. The terrain is smoother in the bias metric but has more
  total variation, i.e. it isn't uniformly better by every measure.
- **Stochastic "bounded detail" component**: measured diversity of exactly
  **0** in the benchmark. In the tested configuration this component
  did not produce the variation it was designed to add.
- **Scope**: this is a single benchmark scenario with no ablation study. The
  numbers above describe one test setup, not a swept evaluation across
  conditions.

## Repository layout

```
/thesis
  MyThesis.pdf       — the submitted thesis (~65 pages)
  latex/             — LaTeX source (MFF UK thesis template)
/unity               — the Unity implementation, as submitted with the thesis
  README.md          — install/usage instructions, parameter reference
  DEVELOPER_DOCUMENTATION.md — architecture, extension guide
/docs
  poster.pdf         — print poster (defence)
  defence_presentation.pdf — thesis defence slide deck
```

## Running the Unity tool

See `unity/README.md` for full instructions. Short version: requires Unity 6+,
open `/unity` as a project in Unity Hub, then either use the `TerrainManager`
component directly on a Unity Terrain object, or open
`Tools > Terrain > Interactive Evolution` for the genetic-algorithm workflow.

## On the four original repos

This thesis's work was previously split across four GitHub repos:

- `Constrained-Procedural-Terrain-Generation`: the actual development repo
  (98 commits, April–May 2025). This is where the Unity tool was actually
  built; it remains the real history of how the implementation evolved, and
  differs in some details from the final submitted version (e.g. it lacks
  the later "deterministic generation" toggle and has a different
  Distance-Falloff parameter). Kept as-is, private.
- `Procedural-Terrain-Generation-with-Genetic-Algorithms`: a later, polished
  snapshot pushed as a single import ("Add all files from original Gitlab
  Repository", July 2025). Verified byte-for-byte identical to the Unity
  project actually submitted with the thesis. This is the version copied
  into `/unity` here. Kept as-is, public.
- `MyThesis`: the LaTeX source, synced from Overleaf (82 commits). This is
  the canonical, versioned history of the thesis text; `/thesis/latex` here
  is a snapshot of its final state, not a replacement for it. Kept as-is,
  private.
- `Terminal-Procedural-Terrain-Generation`: **not** thesis work. A separate
  C# console application (Random/Perlin/midpoint-displacement terrain
  rendered as ASCII/colour blocks in a terminal), committed February–March
  2025, before the thesis's April 2025 start date. No shared code with the
  Unity project, no constraint/fixed-point handling, no genetic algorithm.
  Thematically adjacent, but a different (earlier) course project. Left out
  of this consolidation and left as its own repo.

This repository is a curated presentation copy, not a replacement for those
four. Their commit histories are the real development record and are left
untouched.

## Written up

A plain-English summary, with the paper's weaker results stated next to the
good ones: https://oliverwakeford.com/projects/terrain-generation

## License

The Unity code (`/unity`) is MIT-licensed, see `unity/LICENSE`. The thesis
text and LaTeX source are not under an open license; usual academic-work
copyright applies to Oliver Wakeford and Charles University.
