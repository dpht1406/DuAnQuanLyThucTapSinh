# Import performance: before vs after

Measurements call `StudentService.ImportStudentsAsync` directly with EF Core
InMemory, using one fresh database for each run. This is the same service-level
method and test data path used for the before measurements; it is not an API or
SQL Server benchmark. Times are wall-clock milliseconds from `Stopwatch`.

| CSV rows | Before min / median / max (ms) | After min / median / max (ms) | Median change |
|---:|---:|---:|---:|
| 1,000 | 54.58 / 77.64 / 824.02 | 42.49 / 50.35 / 455.93 | -27.29 ms (-35.2%) |
| 5,000 | 225.13 / 279.58 / 384.56 | 254.75 / 261.95 / 307.15 | -17.63 ms (-6.3%) |

The 5,000-row minimum was 29.62 ms (13.2%) slower after the changes, although
the median and maximum were lower. Measurements vary substantially between
fresh-process/cold and warm runs, so these three-run results are indicative and
not a statistically controlled benchmark. Raw measurements:
[before](./before/perf.csv) and [after](./after/perf.csv).
