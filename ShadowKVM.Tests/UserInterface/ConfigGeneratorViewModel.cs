namespace ShadowKVM.Tests;

public class ConfigGeneratorViewModelTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var model = new ConfigGeneratorViewModel();

        Assert.Equal(0, model.Current);
        Assert.Equal(1, model.Maximum);
    }

    [Fact]
    public void Progress_UpdatesProperties()
    {
        var model = new ConfigGeneratorViewModel();

        // Progress.Report works asynchronously, we need to wait until it finishes
        var finishedEvent = new AutoResetEvent(false);
        model.Progress.ProgressChanged += (_, _) => finishedEvent.Set();

        IProgress<ConfigGeneratorStatus> progress = model.Progress;
        progress.Report(new ConfigGeneratorStatus { Current = 5, Maximum = 7 });

        // Wait for progress to be reported
        Assert.True(finishedEvent.WaitOne(TimeSpan.FromSeconds(5)));

        Assert.Equal(5, model.Current);
        Assert.Equal(7, model.Maximum);
    }

    [Fact]
    public void Current_TriggersEvent()
    {
        var model = new ConfigGeneratorViewModel();

        var called = false;
        model.PropertyChanged += (sender, args) =>
        {
            Assert.Equal(model, sender);
            Assert.Equal(nameof(model.Current), args.PropertyName);
            Assert.Equal(11, model.Current);

            called = true;
        };

        model.Current = 11;

        Assert.True(called);
    }

    [Fact]
    public void Maximum_TriggersEvent()
    {
        var model = new ConfigGeneratorViewModel();

        var called = false;
        model.PropertyChanged += (sender, args) =>
        {
            Assert.Equal(model, sender);
            Assert.Equal(nameof(model.Maximum), args.PropertyName);
            Assert.Equal(13, model.Maximum);

            called = true;
        };

        model.Maximum = 13;

        Assert.True(called);
    }

    [Fact]
    public void GenerationCompleted_DoesNotTrigger()
    {
        var model = new ConfigGeneratorViewModel();

        var called = false;
        model.GenerationCompleted += (sender) =>
        {
            called = true;
        };

        model.Maximum = 17;
        model.Current = 13;

        Assert.False(called);
    }

    [Fact]
    public void GenerationCompleted_Triggers()
    {
        var model = new ConfigGeneratorViewModel();

        var called = false;
        model.GenerationCompleted += (sender) =>
        {
            called = true;
        };

        model.Maximum = 17;
        model.Current = 17;

        Assert.True(called);
    }

    [Fact]
    public void GenerationCompleted_WithoutEventHandler()
    {
        var model = new ConfigGeneratorViewModel();

        model.Maximum = 17;
        model.Current = 17;
    }
}
