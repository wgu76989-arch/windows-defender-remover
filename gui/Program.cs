using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DefenderRemoverGUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Application.StartupPath
                    });
                }
                catch { }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static bool IsAdministrator()
        {
            using (var id = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    class MainForm : Form
    {
        // ---- color palette ----
        static readonly Color Cbg       = Color.FromArgb(240, 242, 245);
        static readonly Color Cpanel    = Color.White;
        static readonly Color Cprimary  = Color.FromArgb(0, 110, 210);
        static readonly Color CprimaryH = Color.FromArgb(0, 90, 180);
        static readonly Color Cdanger   = Color.FromArgb(210, 50, 55);
        static readonly Color CdangerH  = Color.FromArgb(180, 40, 45);
        static readonly Color Ctext     = Color.FromArgb(32, 32, 32);
        static readonly Color Csub      = Color.FromArgb(110, 116, 128);
        static readonly Color Csuccess  = Color.FromArgb(16, 130, 16);
        static readonly Color Cwarn     = Color.FromArgb(180, 120, 0);
        static readonly Color Cborder   = Color.FromArgb(220, 224, 230);
        static readonly Color Clog      = Color.FromArgb(45, 48, 55);

        // ---- fonts ----
        static readonly Font Ftitle  = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        static readonly Font Fver    = new Font("Microsoft YaHei UI", 9F);
        static readonly Font Fbody   = new Font("Microsoft YaHei UI", 9.5F);
        static readonly Font Fbold   = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        static readonly Font Fwarn   = new Font("Microsoft YaHei UI", 9F);
        static readonly Font Fbtn    = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        static readonly Font Flog    = new Font("Consolas", 9F);
        static readonly Font Fstatus = new Font("Microsoft YaHei UI", 9F);

        // ---- controls ----
        RadioButton rb1, rb2, rb3;
        Label d1, d2, d3;
        CheckBox chkRestore, chkReboot;
        Button btnExec, btnReboot;
        ProgressBar prog;
        Label lblStatus;
        RichTextBox txtLog;

        string workDir;
        bool running = false;
        int totalSteps, curStep;

        public MainForm()
        {
            workDir = Application.StartupPath;
            BuildUI();
            VerifyFiles();
        }

        void BuildUI()
        {
            Text = "Windows Defender \u5378\u8f7d\u5de5\u5177";
            Size = new Size(560, 690);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Cbg;
            ForeColor = Ctext;

            // ---- header ----
            var header = new Panel { Location = new Point(0, 0), Size = new Size(560, 60), BackColor = Cpanel };
            header.Paint += (s, e) =>
            {
                using (var pen = new Pen(Cborder))
                    e.Graphics.DrawLine(pen, 0, 59, 560, 59);
            };
            var pic = new PictureBox
            {
                Location = new Point(25, 14),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            try { pic.Image = Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(); }
            catch { }
            var title = new Label
            {
                Text = "Windows Defender \u5378\u8f7d\u5de5\u5177",
                Font = Ftitle, ForeColor = Ctext,
                Location = new Point(68, 12), AutoSize = true,
                BackColor = Color.Transparent
            };
            var ver = new Label
            {
                Text = "v13.1",
                Font = Fver, ForeColor = Csub,
                Location = new Point(68, 36), AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.AddRange(new Control[] { pic, title, ver });

            // ---- warning ----
            var warn = new Panel
            {
                Location = new Point(20, 72), Size = new Size(520, 40),
                BackColor = Color.FromArgb(255, 248, 230)
            };
            var warnIcon = new Label
            {
                Text = "\u26a0", Font = new Font("Segoe UI Symbol", 14F),
                ForeColor = Cwarn, Location = new Point(10, 8),
                AutoSize = true, BackColor = Color.Transparent
            };
            var warnText = new Label
            {
                Text = "\u6b64\u5de5\u5177\u5c06\u6c38\u4e45\u79fb\u9664 Windows \u5b89\u5168\u7ec4\u4ef6\u3002\u5efa\u8bae\u64cd\u4f5c\u524d\u521b\u5efa\u7cfb\u7edf\u8fd8\u539f\u70b9\uff0c\u64cd\u4f5c\u540e\u9700\u91cd\u542f\u7535\u8111\u3002",
                Font = Fwarn, ForeColor = Color.FromArgb(120, 80, 0),
                Location = new Point(35, 12), Size = new Size(475, 20),
                BackColor = Color.Transparent
            };
            warn.Controls.AddRange(new Control[] { warnIcon, warnText });

            // ---- mode label ----
            var modeLabel = new Label
            {
                Text = "\u8bf7\u9009\u62e9\u64cd\u4f5c\u6a21\u5f0f\uff1a",
                Font = Fbold, ForeColor = Ctext,
                Location = new Point(20, 122), AutoSize = true, BackColor = Cbg
            };

            // ---- mode panel ----
            var modePanel = new Panel
            {
                Location = new Point(20, 148), Size = new Size(520, 175), BackColor = Cpanel
            };

            rb1 = MkRadio("\u5b8c\u5168\u79fb\u9664 Windows Defender\uff08\u63a8\u8350\uff09", new Point(15, 10), true);
            d1  = MkDesc("\u79fb\u9664 Defender \u9632\u75c5\u6bd2\u5f15\u64ce + Windows \u5b89\u5168\u4e2d\u5fc3", new Point(35, 33));
            rb2 = MkRadio("\u4ec5\u79fb\u9664\u9632\u75c5\u6bd2\u5f15\u64ce", new Point(15, 65), false);
            d2  = MkDesc("\u4fdd\u7559\u5b89\u5168\u4e2d\u5fc3\u754c\u9762\uff08\u7cfb\u7edf\u66f4\u65b0\u540e\u53ef\u80fd\u6062\u590d\uff09", new Point(35, 88));
            rb3 = MkRadio("\u79fb\u9664 Defender \u6b8b\u7559\u6587\u4ef6", new Point(15, 120), false);
            d3  = MkDesc("\u5f3a\u5236\u5220\u9664 Defender \u5b89\u88c5\u76ee\u5f55\u548c\u6587\u4ef6", new Point(35, 143));

            modePanel.Controls.AddRange(new Control[] { rb1, d1, rb2, d2, rb3, d3 });

            // ---- checkboxes ----
            chkRestore = MkCheck("\u64cd\u4f5c\u524d\u521b\u5efa\u7cfb\u7edf\u8fd8\u539f\u70b9\uff08\u5f3a\u70c8\u5efa\u8bae\uff09", new Point(20, 338), true);
            chkReboot  = MkCheck("\u64cd\u4f5c\u5b8c\u6210\u540e\u81ea\u52a8\u91cd\u542f\u7535\u8111", new Point(20, 363), false);

            // ---- execute button ----
            btnExec = MkBtn("\u25b6  \u5f00\u59cb\u6267\u884c", new Point(20, 395), new Size(520, 42), Cprimary, CprimaryH);
            btnExec.Click += BtnExec_Click;

            // ---- progress ----
            prog = new ProgressBar
            {
                Location = new Point(20, 448), Size = new Size(520, 16),
                Style = ProgressBarStyle.Continuous
            };
            lblStatus = new Label
            {
                Text = "\u5c31\u7eea", Font = Fstatus, ForeColor = Csub,
                Location = new Point(20, 470), Size = new Size(400, 18), BackColor = Cbg
            };

            // ---- log ----
            var logLabel = new Label
            {
                Text = "\u6267\u884c\u65e5\u5fd7", Font = Fbody, ForeColor = Csub,
                Location = new Point(20, 495), AutoSize = true, BackColor = Cbg
            };
            txtLog = new RichTextBox
            {
                Location = new Point(20, 518), Size = new Size(520, 105),
                Font = Flog, BackColor = Clog, ForeColor = Color.FromArgb(220, 224, 230),
                BorderStyle = BorderStyle.FixedSingle, ReadOnly = true, WordWrap = false
            };

            // ---- reboot button ----
            btnReboot = MkBtn("\u7acb\u5373\u91cd\u542f\u7535\u8111", new Point(20, 632), new Size(520, 38), Cdanger, CdangerH);
            btnReboot.Visible = false;
            btnReboot.Click += (s, e) => Process.Start("shutdown.exe", "/r /f /t 3");

            Controls.AddRange(new Control[] { header, warn, modeLabel, modePanel,
                chkRestore, chkReboot, btnExec, prog, lblStatus, logLabel, txtLog, btnReboot });
        }

        RadioButton MkRadio(string text, Point loc, bool check)
        {
            return new RadioButton
            {
                Text = text, Font = Fbold, ForeColor = Ctext,
                Location = loc, Size = new Size(490, 24),
                Checked = check, BackColor = Cpanel, FlatStyle = FlatStyle.Flat
            };
        }

        Label MkDesc(string text, Point loc)
        {
            return new Label
            {
                Text = text, Font = Fbody, ForeColor = Csub,
                Location = loc, Size = new Size(470, 20), BackColor = Cpanel
            };
        }

        CheckBox MkCheck(string text, Point loc, bool check)
        {
            return new CheckBox
            {
                Text = text, Font = Fbody, ForeColor = Ctext,
                Location = loc, Size = new Size(400, 22),
                Checked = check, BackColor = Cbg, FlatStyle = FlatStyle.Flat
            };
        }

        Button MkBtn(string text, Point loc, Size sz, Color bg, Color hover)
        {
            var b = new Button
            {
                Text = text, Font = Fbtn, ForeColor = Color.White,
                Location = loc, Size = sz,
                FlatStyle = FlatStyle.Flat, BackColor = bg,
                TextAlign = ContentAlignment.MiddleCenter
            };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => b.BackColor = hover;
            b.MouseLeave += (s, e) => b.BackColor = bg;
            return b;
        }

        void VerifyFiles()
        {
            string pr = Path.Combine(workDir, "PowerRun.exe");
            string rd = Path.Combine(workDir, "Remove_Defender");
            if (!File.Exists(pr) || !Directory.Exists(rd))
            {
                Log("\u26a0 \u672a\u68c0\u6d4b\u5230 PowerRun.exe \u6216\u6ce8\u518c\u8868\u6587\u4ef6\u76ee\u5f55\u3002");
                Log("  \u8bf7\u786e\u4fdd\u672c\u7a0b\u5e8f\u4e0e\u6240\u6709\u914d\u5957\u6587\u4ef6\u5728\u540c\u4e00\u76ee\u5f55\u4e0b\u3002");
                Log("  \u5de5\u4f5c\u76ee\u5f55\uff1a" + workDir);
                btnExec.Enabled = false;
                btnExec.BackColor = Csub;
            }
            else
            {
                Log("\u2713 \u6587\u4ef6\u9a8c\u8bc1\u901a\u8fc7\uff0c\u5c31\u7eea\u3002");
            }
        }

        void Log(string msg)
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke(new Action(() => Log(msg)));
            else
            {
                txtLog.SelectionColor = msg.StartsWith("\u2713") ? Color.FromArgb(100, 220, 120) :
                                       msg.StartsWith("\u26a0") ? Color.FromArgb(240, 200, 80) :
                                       msg.StartsWith("\u2716") ? Color.FromArgb(240, 100, 100) :
                                       Color.FromArgb(210, 215, 225);
                txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n");
                txtLog.ScrollToCaret();
            }
        }

        void SetStatus(string s)
        {
            if (lblStatus.InvokeRequired) lblStatus.Invoke(new Action(() => SetStatus(s)));
            else lblStatus.Text = s;
        }

        void SetProg(int v)
        {
            if (prog.InvokeRequired) prog.Invoke(new Action(() => SetProg(v)));
            else prog.Value = Math.Max(0, Math.Min(100, v));
        }

        void SetProgStyle(ProgressBarStyle st)
        {
            if (prog.InvokeRequired) prog.Invoke(new Action(() => SetProgStyle(st)));
            else prog.Style = st;
        }

        void SetBtnEnabled(bool en)
        {
            if (btnExec.InvokeRequired) btnExec.Invoke(new Action(() => SetBtnEnabled(en)));
            else btnExec.Enabled = en;
        }

        void ShowRebootBtn()
        {
            if (btnReboot.InvokeRequired) btnReboot.Invoke(new Action(() => ShowRebootBtn()));
            else btnReboot.Visible = true;
        }

        int Run(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(workDir, exe),
                    Arguments = args, WorkingDirectory = workDir,
                    UseShellExecute = false, CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var p = Process.Start(psi)) { p.WaitForExit(); return p.ExitCode; }
            }
            catch (Exception ex)
            {
                Log("  \u2716 \u5f02\u5e38\uff1a" + ex.Message);
                return -1;
            }
        }

        async void BtnExec_Click(object sender, EventArgs e)
        {
            if (running) return;

            string mode = rb1.Checked ? "\u5b8c\u5168\u79fb\u9664 Windows Defender" :
                          rb2.Checked ? "\u4ec5\u79fb\u9664\u9632\u75c5\u6bd2\u5f15\u64ce" :
                                        "\u79fb\u9664 Defender \u6b8b\u7559\u6587\u4ef6";

            if (DialogResult.No == MessageBox.Show(
                "\u60a8\u9009\u62e9\u4e86\u3010" + mode + "\u3011\u3002\n\n\u6b64\u64cd\u4f5c\u5c06\u4fee\u6539\u7cfb\u7edf\u6ce8\u518c\u8868\uff0c\u53ef\u80fd\u4e0d\u53ef\u9006\u3002\n\u786e\u5b9a\u8981\u7ee7\u7eed\u5417\uff1f",
                "\u786e\u8ba4\u64cd\u4f5c",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2))
                return;

            running = true;
            SetBtnEnabled(false);
            btnReboot.Visible = false;
            SetProg(0);
            txtLog.Clear();

            await Task.Run(() => DoWork());

            running = false;
            SetBtnEnabled(true);
            SetProg(100);

            if (chkReboot.Checked)
            {
                Log("\u5c06\u5728 5 \u79d2\u540e\u91cd\u542f\u7535\u8111...");
                Process.Start("shutdown.exe", "/r /f /t 5");
            }
            else
            {
                ShowRebootBtn();
            }
        }

        void DoWork()
        {
            curStep = 0;
            totalSteps = CalcSteps();
            SetProgStyle(ProgressBarStyle.Continuous);

            if (chkRestore.Checked)
            {
                SetStatus("\u6b63\u5728\u521b\u5efa\u7cfb\u7edf\u8fd8\u539f\u70b9...");
                Log("\u6b63\u5728\u521b\u5efa\u7cfb\u7edf\u8fd8\u539f\u70b9...");
                SetProgStyle(ProgressBarStyle.Marquee);
                CreateRestorePoint();
                curStep++;
                SetProg(curStep * 100 / totalSteps);
                Log("\u2713 \u7cfb\u7edf\u8fd8\u539f\u70b9\u521b\u5efa\u5b8c\u6210");
                SetProgStyle(ProgressBarStyle.Continuous);
            }

            if (rb1.Checked) DoFullRemoval();
            else if (rb2.Checked) DoAntivirusOnly();
            else if (rb3.Checked) DoFileRemoval();

            SetStatus("\u64cd\u4f5c\u5b8c\u6210");
            Log("");
            Log("\u2713 \u5168\u90e8\u64cd\u4f5c\u5b8c\u6210\uff0c\u5efa\u8bae\u91cd\u542f\u7535\u8111\u4ee5\u4f7f\u66f4\u6539\u751f\u6548\u3002");
        }

        int CalcSteps()
        {
            int n = 0;
            if (chkRestore.Checked) n++;
            if (rb1.Checked) n += 1 + CountRegs("Remove_Defender") * 2 + CountRegs("Remove_SecurityComp");
            else if (rb2.Checked) n += CountRegs("Remove_Defender") * 2;
            else if (rb3.Checked) n += 4;
            return n > 0 ? n : 1;
        }

        int CountRegs(string dir)
        {
            string p = Path.Combine(workDir, dir);
            return Directory.Exists(p) ? Directory.GetFiles(p, "*.reg", SearchOption.TopDirectoryOnly).Length : 0;
        }

        void CreateRestorePoint()
        {
            try
            {
                Run("powershell.exe", "-NoProfile -Command \"Enable-ComputerRestore -Drive C:\\\"");
                Run("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"Checkpoint-Computer -Description 'DefenderRemover' -RestorePointType MODIFY_SETTINGS\"");
            }
            catch (Exception ex)
            {
                Log("  \u2716 \u521b\u5efa\u8fd8\u539f\u70b9\u5931\u8d25\uff1a" + ex.Message);
            }
        }

        void DoFullRemoval()
        {
            SetStatus("\u6b63\u5728\u79fb\u9664 Windows \u5b89\u5168\u4e2d\u5fc3...");
            Log("\u6b63\u5728\u79fb\u9664 Windows \u5b89\u5168\u4e2d\u5fc3 UWP \u5e94\u7528...");
            SetProgStyle(ProgressBarStyle.Marquee);
            Run("PowerRun.exe", "powershell.exe -noprofile -executionpolicy bypass -file \"RemoveSecHealthApp.ps1\"");
            curStep++;
            SetProg(curStep * 100 / totalSteps);
            Log("\u2713 \u5b89\u5168\u4e2d\u5fc3\u5e94\u7528\u5df2\u79fb\u9664");
            SetProgStyle(ProgressBarStyle.Continuous);

            SetStatus("\u6b63\u5728\u5e94\u7528\u6ce8\u518c\u8868\u4fee\u6539...");
            ApplyRegFiles("Remove_Defender", true);
            ApplyRegFiles("Remove_SecurityComp", false);
            Log("\u2713 \u6ce8\u518c\u8868\u4fee\u6539\u5b8c\u6210");
        }

        void DoAntivirusOnly()
        {
            SetStatus("\u6b63\u5728\u5e94\u7528\u6ce8\u518c\u8868\u4fee\u6539...");
            ApplyRegFiles("Remove_Defender", true);
            Log("\u2713 \u6ce8\u518c\u8868\u4fee\u6539\u5b8c\u6210");
        }

        void DoFileRemoval()
        {
            SetStatus("\u6b63\u5728\u79fb\u9664 Defender \u6587\u4ef6...");
            string[] dirs = new string[]
            {
                @"C:\ProgramData\Microsoft\Windows Defender",
                @"C:\Program Files\Windows Defender",
                @"C:\Program Files (x86)\Windows Defender",
                @"C:\Program Files\Windows Defender Advanced Threat Protection"
            };
            for (int i = 0; i < dirs.Length; i++)
            {
                Log("\u6b63\u5728\u5220\u9664 (" + (i + 1) + "/" + dirs.Length + ")\uff1a" + dirs[i]);
                Run("PowerRun.exe", "cmd.exe /c takeown /f \"" + dirs[i] + "\" /r /d y && icacls \"" + dirs[i] + "\" /grant administrators:F /t && rd /s /q \"" + dirs[i] + "\"");
                curStep++;
                SetProg(curStep * 100 / totalSteps);
            }
            Log("\u2713 \u6587\u4ef6\u79fb\u9664\u5b8c\u6210");
        }

        void ApplyRegFiles(string dir, bool doubleApply)
        {
            string p = Path.Combine(workDir, dir);
            if (!Directory.Exists(p)) { Log("  \u26a0 \u627e\u4e0d\u5230\u76ee\u5f55\uff1a" + dir); return; }
            string[] regs = Directory.GetFiles(p, "*.reg", SearchOption.TopDirectoryOnly);
            Log("\u6b63\u5728\u5bfc\u5165 " + dir + " \u6ce8\u518c\u8868\u6587\u4ef6\uff08\u5171 " + regs.Length + " \u4e2a\uff09...");
            for (int i = 0; i < regs.Length; i++)
            {
                string name = Path.GetFileName(regs[i]);
                Log("  (" + (i + 1) + "/" + regs.Length + ") " + name);
                Run("PowerRun.exe", "regedit.exe /s \"" + regs[i] + "\"");
                if (doubleApply) { Run("regedit.exe", "/s \"" + regs[i] + "\""); curStep += 2; }
                else { curStep++; }
                SetProg(curStep * 100 / totalSteps);
            }
        }
    }
}
