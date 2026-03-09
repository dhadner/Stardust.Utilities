namespace Stardust.Utilities.Tests;

/// <summary>
/// Detects whether code is running in a Continuous Integration environment.
/// Used to conditionally skip performance tests that produce unreliable results in CI.
/// </summary>
public static class CiEnvironmentDetector
{
    /// <summary>
    /// Gets whether the current execution is in a CI environment.
    /// Checks common CI environment variables from popular CI/CD systems.
    /// </summary>
    public static bool IsRunningInCi { get; } = DetectCiEnvironment();

    /// <summary>
    /// Gets the skip reason if running in CI, or null if running locally.
    /// Use this with xUnit's Skip property for conditional skipping.
    /// </summary>
    public static string? SkipInCi =>
        IsRunningInCi ? "Performance tests are skipped in CI environments due to variable runner performance." : null;

    private static bool DetectCiEnvironment()
    {
        // Check common CI environment variables
        // These are set by various CI/CD systems
        string?[] ciIndicators =
        [
            Environment.GetEnvironmentVariable("CI"),                    // GitHub Actions, GitLab CI, Travis CI, CircleCI
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),        // GitHub Actions specifically
            Environment.GetEnvironmentVariable("TF_BUILD"),              // Azure DevOps
            Environment.GetEnvironmentVariable("JENKINS_URL"),           // Jenkins
            Environment.GetEnvironmentVariable("TRAVIS"),                // Travis CI
            Environment.GetEnvironmentVariable("CIRCLECI"),              // CircleCI
            Environment.GetEnvironmentVariable("GITLAB_CI"),             // GitLab CI
            Environment.GetEnvironmentVariable("BUILDKITE"),             // Buildkite
            Environment.GetEnvironmentVariable("TEAMCITY_VERSION"),      // TeamCity
            Environment.GetEnvironmentVariable("APPVEYOR"),              // AppVeyor
            Environment.GetEnvironmentVariable("CODEBUILD_BUILD_ID"),    // AWS CodeBuild
        ];

        return ciIndicators.Any(indicator => !string.IsNullOrEmpty(indicator));
    }
}
