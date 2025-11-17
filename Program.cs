namespace WinTrimmy;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Ensure only one instance runs
        using var mutex = new Mutex(true, "WinTrimmy-SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("WinTrimmy is already running.", "WinTrimmy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplicationContext());
    }
}
