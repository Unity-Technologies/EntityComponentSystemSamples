# Performance Test Viewer
The performance test viewer is a tool that can help you assess changes in performance by comparing local testing results. It's designed to help you avoid obvious mistakes by pointing out performance regressions. Performance testing on local machines (which are far from what you'd call a carefully controlled environment) always comes with a hefty amount of noise. Additionally, there is only so much you can learn by applying statistical tests as this tool is doing. The data provided by this tool is the start of a conversation, not an end.

Even when there are no regressions visible in this tool, you might later find out that you have regressions on the same platform on CI (in a more tightly controlled environment). Similarly, potential improvements might vanish. This tool is merely a first line of defense.

## How to use the tool
1. Open the tool from `Window/Analysis/Performance Test Viewer`.
2. Use the test runner inside of Unity to run performance tests of your choice.
3. Look at the tool, it should automatically load the results. Click "Store as Baseline" in the upper left corner.
4. Make your code changes and re-run the performance tests.
5. The tool will now have loaded the new test data and compare it to the baseline you previously recorded.
6. Marvel at the fact that almost everything has a helpful tooltip.

Use the context menu of the tool (top right corner of the window) to export your results as Markdown that you can use as part of a PR on GitHub.

## What do I see?
Each test is classified as either a regression, an improvement, or as inconclusive. Behind the scenes, a statistical test called the "T-test" is applied to estimate the difference between the mean of the baseline results vs. the new results for this particular test. The result of this statistical test is used to classify the tests and to give a "95% confidence interval" for the change in means. Negative numbers are improvements, positive numbers are regressions.

If these terms don't make any sense to you, I'd recommend watching [this talk](https://www.youtube.com/watch?v=fl9V0U2SGeI) to get a rough idea, followed by taking a proper course on statistics. The short gist is that the confidence interval is a tool to help you gauge what the likely change in performance will be. The quality of these estimates is mainly determined by two things: The quality of the inputs (e.g. when your testing environment is very noisy, the estimate will be garbage) and the number of samples taken for a performance test.

Besides the pure numbers, you can see the distribution of the baseline and the new results drawn in to different colors. It is vitally important to not just look at the results of a statistical test but to take a good look at the distribution of the samples to at least get a good idea for what is shown.

## Testing Methods
The testing methodology is as follows:
1. We remove the top 5% of the samples. Empirically, the spikes of the distribution contain little information and will distort the mean. We'd actually like to test the median instead, but there are other problems related to that.
2. We apply a two-tailed T-test to the data with p=0.05. No correction is made for the number of tests performed since a risk of false-detections is acceptable when looking for regressions specifically. Results with p > 0.05 are marked as inconclusive.
3. We compute a 95% CI for each T-test result. If that interval includes 0, it is discarded as inconclusive. If the change is at least 1% or the far-end of the CI is more than 10% off (measured relative to the baseline mean), it is marked as a regression. Similarly, improvements are only registered if they are at least 10% better since improvements below that might just as well be [noise](https://www.youtube.com/watch?v=r-TLSBdHe1A).

Note here that purely on a statistical level, the use of the T-test is not entirely uncontroversial. Performance testing results are likely skewed towards a minimum value and will generally not be normal. Furthermore, measuring a change in means is not ideal: We'd really like to measure the change in minimum or median, but both of these are difficult to estimate. Bootstrapping might be an approach.