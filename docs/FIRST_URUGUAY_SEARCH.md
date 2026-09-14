# First Uruguay-to-HydroLAKES search

## Outcome

The first end-to-end run completed successfully on 2026-09-11. It scanned all
1,427,688 HydroLAKES polygon records, retained the 1,000 candidates nearest to
Uruguay by rotation-invariant compactness and elongation, and compared those
candidates using rotation-optimized Intersection over Union (IoU).

No records failed during prefiltering or fine matching. Total elapsed time was
2 minutes 8.86 seconds on the development machine.

The best result in this configuration is unnamed HydroLAKES feature `188254`
in Canada, with an IoU score of `0.88748270` at a rotation of `347.50` degrees.

## Top 20

| Rank | HydroLAKES ID | Country | Area km2 | IoU | Rotation | Type |
| ---: | ---: | --- | ---: | ---: | ---: | --- |
| 1 | 188254 | Canada | 0.15 | 88.75% | 347.50 | Lake |
| 2 | 627110 | Canada | 0.12 | 86.66% | 112.25 | Lake |
| 3 | 129005 | Russia | 3.68 | 86.33% | 126.50 | Lake |
| 4 | 672914 | United States of America | 0.39 | 86.31% | 261.50 | Lake |
| 5 | 849438 | Canada | 0.39 | 86.16% | 288.50 | Lake |
| 6 | 584250 | Canada | 0.12 | 85.55% | 129.25 | Lake |
| 7 | 58256 | Canada | 1.35 | 85.46% | 345.50 | Lake |
| 8 | 133537 | Russia | 7.02 | 85.44% | 277.00 | Lake |
| 9 | 896295 | Canada | 0.33 | 85.31% | 322.00 | Lake |
| 10 | 425640 | Canada | 0.15 | 85.30% | 183.00 | Lake |
| 11 | 1319133 | Kazakhstan | 0.83 | 84.93% | 291.50 | Lake |
| 12 | 710436 | Canada | 0.21 | 84.90% | 63.75 | Lake |
| 13 | 106737 | United States of America | 2.64 | 84.75% | 14.50 | Lake |
| 14 | 342058 | Canada | 0.34 | 84.65% | 42.00 | Lake |
| 15 | 134774 | Russia | 5.25 | 84.65% | 188.75 | Lake |
| 16 | 76124 | Canada | 2.08 | 84.63% | 91.50 | Lake |
| 17 | 470305 | Canada | 0.15 | 84.58% | 109.25 | Lake |
| 18 | 694408 | United States of America | 0.98 | 84.54% | 177.75 | Lake |
| 19 | 1005467 | Canada | 0.11 | 84.43% | 171.25 | Lake |
| 20 | 205907 | Canada | 0.10 | 84.37% | 143.00 | Lake |

All 20 features are unnamed in HydroLAKES and have `Lake_type=1`. HydroLAKES
uses type 1 for lakes, type 2 for reservoirs, and type 3 for regulated natural
lakes. Type 1 is also the dataset default, so it can include unidentified small
human-made or regulated water bodies.

## Reproduction

```powershell
dotnet run --project src/GetMyLake.Cli -- search-uruguay `
  --prefilter 1000 `
  --top 20 `
  --output results/uruguay-lakes.csv
```

Dataset archive SHA-256 values:

```text
Natural Earth: CE1AC7036499A0EDD641FBC093CD209A98F96A49D2ECA8480AAACAD35138A7F6
HydroLAKES:    9EF63498569DD7CCD0B8D8852DA026E009A75F42D293F00E483F3B9FCFE8E1F9
```

## Interpretation boundary

This is an exploratory ranking, not a mathematical proof that feature `188254`
is the globally optimal IoU match. Fine IoU was applied to the 1,000 candidates
selected by the inexpensive descriptors rather than to all 1.4 million shapes.
Automated comparison PNGs were added and visually reviewed on 2026-09-14. Each
image shows Uruguay, the rotated lake, and a transparent overlay at one shared
scale. The broader prefilter sensitivity runs remain the next check for ranking
stability.
