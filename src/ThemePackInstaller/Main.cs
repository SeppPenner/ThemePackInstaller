// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Main.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ThemePackInstaller;

/// <summary>
/// The main form.
/// </summary>
public partial class Main : Form
{
    /// <summary>
    /// The file name of the theme pack that is installed.
    /// </summary>
    private const string ThemePackFileName = "Anti-BVB.themepack";

    /// <summary>
    /// Initializes a new instance of the <see cref="Main"/> class.
    /// </summary>
    public Main()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Creates the parameters.
    /// </summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80;
            return cp;
        }
    }

    /// <summary>
    /// Handles the main load event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void MainLoad(object sender, EventArgs e)
    {
        this.Visible = false;

        try
        {
            var themePackFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ThemePackFileName);

            // UseShellExecute is required because the theme pack is a data file and not an executable.
            // Without it, Process.Start fails with a Win32Exception on .NET.
            var startInfo = new ProcessStartInfo(themePackFile)
            {
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            var text = $"{ex.Message}{Environment.NewLine}{Environment.NewLine}{ex.StackTrace}";
            MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        this.Close();
    }
}
