using MetroSuite;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using System;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.IO;
using NAudio.Midi;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.Cryptography;
using System.Threading;

public partial class MainForm : MetroForm
{
    public MainForm()
    {
        InitializeComponent();
        CheckForIllegalCrossThreadCalls = false;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        guna2Button5.BorderRadius = 3;
        guna2Button6.BorderRadius = 3;
    }

    private void guna2Button3_Click(object sender, System.EventArgs e)
    {
        listBox1.Items.Clear();
    }

    private void guna2Button1_Click(object sender, System.EventArgs e)
    {
        if (openFileDialog1.ShowDialog().Equals(DialogResult.OK))
        {
            foreach (string fileName in openFileDialog1.FileNames)
            {
                string newFileName = fileName.ToLower();

                if (!listBox1.Items.Contains(newFileName))
                {
                    listBox1.Items.Add(newFileName);
                }
            }
        }
    }

    private void guna2Button2_Click(object sender, System.EventArgs e)
    {
        if (listBox1.SelectedItems.Count > 0)
        {
            List<string> items = new List<string>();

            foreach (string item in listBox1.SelectedItems)
            {
                items.Add(item.ToString());
            }

            foreach (string item in items)
            {
                listBox1.Items.Remove(item);
            }
        }
    }

    private void guna2Button4_Click(object sender, System.EventArgs e)
    {
        try
        {
            if (listBox1.Items.Count == 0)
            {
                return;
            }

            if (saveFileDialog1.ShowDialog().Equals(DialogResult.OK))
            {
                if (System.IO.Directory.Exists("temp_audio"))
                {
                    System.IO.Directory.Delete("temp_audio", true);
                }

                System.IO.Directory.CreateDirectory("temp_audio");
                int currentFile = 1;

                foreach (string audioFile in listBox1.Items)
                {
                    System.IO.File.Copy(audioFile, $"temp_audio\\temp_{currentFile}.wav");
                    WaveFileReader reader = new WaveFileReader(Application.StartupPath + $"\\temp_audio\\temp_{currentFile}.wav");
                    CamelWaveProvider provider = new CamelWaveProvider(reader.ToSampleProvider(), guna2CheckBox2.Checked);
                    WaveFormat outFormat = new WaveFormat(22050, 16, 1);
                    MediaFoundationResampler resampler = new MediaFoundationResampler(provider.ToWaveProvider(), outFormat);
                    WaveFileWriter.CreateWaveFile16(Application.StartupPath + $"\\temp_audio\\new_1{currentFile}.wav", resampler.ToSampleProvider());
                    reader.Close();
                    reader.Dispose();
                    currentFile++;
                }

                currentFile = 0;

                foreach (string file in System.IO.Directory.GetFiles("temp_audio"))
                {
                    if (System.IO.Path.GetFileNameWithoutExtension(file).StartsWith("temp_"))
                    {
                        if (guna2CheckBox1.Checked)
                        {
                            currentFile++;
                            float max = 0;

                            using (var reader = new AudioFileReader(Application.StartupPath + $"\\temp_audio\\new_1{currentFile}.wav"))
                            {
                                float[] buffer = new float[reader.WaveFormat.SampleRate];
                                int read;

                                do
                                {
                                    read = reader.Read(buffer, 0, buffer.Length);

                                    for (int n = 0; n < read; n++)
                                    {
                                        var abs = Math.Abs(buffer[n]);
                                        if (abs > max) max = abs;
                                    }
                                }
                                while (read > 0);

                                if (!(max == 0 || max > 1.0f))
                                {
                                    reader.Position = 0;
                                    reader.Volume = 1.0f / max;
                                    WaveFileWriter.CreateWaveFile(Application.StartupPath + $"\\temp_audio\\{currentFile}.wav", reader);
                                    reader.Close();
                                    reader.Dispose();
                                }
                                else
                                {
                                    reader.Close();
                                    reader.Dispose();
                                    System.IO.File.Copy(file, Application.StartupPath + $"\\temp_audio\\{currentFile}.wav");
                                }
                            }
                        }
                        else
                        {
                            currentFile++;
                            System.IO.File.Copy(file, Application.StartupPath + $"\\temp_audio\\{currentFile}.wav");
                        }
                    }
                }

                foreach (string file in System.IO.Directory.GetFiles("temp_audio"))
                {
                    if (System.IO.Path.GetFileNameWithoutExtension(file).StartsWith("temp_") || System.IO.Path.GetFileNameWithoutExtension(file).StartsWith("new_"))
                    {
                        System.IO.File.Delete(file);
                    }
                }

                bool started = false;
                byte[] splitted = Encoding.UTF8.GetBytes("|CAMEL_TTS|");
                byte[] theData = new byte[] { };

                foreach (string file in System.IO.Directory.GetFiles("temp_audio"))
                {
                    byte[] readFile = System.IO.File.ReadAllBytes(file);
                    int fileLength = readFile.Length;
                    byte[] bytesLength = BitConverter.GetBytes(fileLength);

                    if (!started)
                    {
                        theData = Combine(splitted, bytesLength, readFile);
                        started = true;
                    }
                    else
                    {
                        theData = Combine(theData, splitted, bytesLength, readFile);
                    }
                }

                System.IO.File.WriteAllBytes(saveFileDialog1.FileName, Compress(theData));
                System.IO.Directory.Delete("temp_audio", true);
                MessageBox.Show("Succesfully trained and exported your model!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch
        {
            MessageBox.Show("Failed to train and export your model.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static byte[] Compress(byte[] data)
    {
        using (MemoryStream compressedStream = new MemoryStream())
        {
            using (GZipStream zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
    }

    public static byte[] Decompress(byte[] data)
    {
        using (MemoryStream compressedStream = new MemoryStream(data))
        {
            using (GZipStream zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            {
                using (MemoryStream resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }
    }

    public static byte[] Combine(params byte[][] arrays)
    {
        byte[] ret = new byte[arrays.Sum(x => x.Length)];
        int offset = 0;

        foreach (byte[] data in arrays)
        {
            Buffer.BlockCopy(data, 0, ret, offset, data.Length);
            offset += data.Length;
        }

        return ret;
    }

    private void guna2Button6_Click(object sender, System.EventArgs e)
    {
        try
        {
            if (guna2RadioButton1.Checked)
            {
                if (System.IO.File.Exists("Resources\\English Model V1\\generated.wav"))
                {
                    System.IO.File.Delete("Resources\\English Model V1\\generated.wav");
                }

                System.IO.File.WriteAllText("Resources\\English Model V1\\text_to_read.txt", guna2TextBox2.Text);
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();
                cmd.StandardInput.WriteLine("cd \"" + Application.StartupPath + "\\Resources\\English Model V1\"");
                cmd.StandardInput.WriteLine("py new_main_script.py");
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();

                while (!System.IO.File.Exists("Resources\\English Model V1\\generated.wav"))
                {
                    Thread.Sleep(10);
                }

                try
                {
                    cmd.Kill();
                }
                catch
                {

                }

                if (saveFileDialog2.ShowDialog().Equals(DialogResult.OK))
                {
                    System.IO.File.Move(Application.StartupPath + "\\Resources\\English Model V1\\generated.wav", saveFileDialog2.FileName);
                }

                MessageBox.Show("Succesfully generated your audio inference!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (guna2RadioButton2.Checked)
            {
                if (System.IO.File.Exists("Resources\\English Model V2\\generated.wav"))
                {
                    System.IO.File.Delete("Resources\\English Model V2\\generated.wav");
                }

                System.IO.File.WriteAllText("Resources\\English Model V2\\text_to_read.txt", guna2TextBox2.Text);
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.Start();
                cmd.StandardInput.WriteLine("cd \"" + Application.StartupPath + "\\Resources\\English Model V2\"");
                cmd.StandardInput.WriteLine("py main.py");
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();

                while (!System.IO.File.Exists("Resources\\English Model V2\\generated.wav"))
                {
                    Thread.Sleep(10);
                }

                try
                {
                    cmd.Kill();
                }
                catch
                {

                }

                if (saveFileDialog2.ShowDialog().Equals(DialogResult.OK))
                {
                    System.IO.File.Move(Application.StartupPath + "\\Resources\\English Model V2\\generated.wav", saveFileDialog2.FileName);
                }

                MessageBox.Show("Succesfully generated your audio inference!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch
        {
            MessageBox.Show("Failed to generate your audio inference.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void guna2Button5_Click(object sender, System.EventArgs e)
    {
        try
        {
            if (openFileDialog2.ShowDialog().Equals(DialogResult.OK))
            {
                byte[] theBytes = System.IO.File.ReadAllBytes(openFileDialog2.FileName);
                theBytes = Decompress(theBytes);
                int currentFile = 1;

                foreach (string file in System.IO.Directory.GetFiles("Resources\\English Model V2\\tortoise\\voices\\actual"))
                {
                    System.IO.File.Delete(file);
                }

                while (true)
                {
                    try
                    {
                        byte[] header = theBytes.Take(11).ToArray();

                        if (Encoding.UTF8.GetString(header) != "|CAMEL_TTS|")
                        {
                            break;
                        }

                        theBytes = theBytes.Skip(11).ToArray();
                        int fileLength = BitConverter.ToInt32(theBytes.Take(4).ToArray(), 0);

                        if (fileLength <= 0)
                        {
                            break;
                        }

                        theBytes = theBytes.Skip(4).ToArray();
                        byte[] wavFile = theBytes.Take(fileLength).ToArray();
                        theBytes = theBytes.Skip(fileLength).ToArray();

                        System.IO.File.WriteAllBytes($"Resources\\English Model V2\\tortoise\\voices\\actual\\{currentFile}.wav", wavFile);
                        currentFile++;
                    }
                    catch
                    {
                        break;
                    }
                }

                if (currentFile == 1)
                {
                    MessageBox.Show("The specified model is not valid.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    System.IO.File.WriteAllText("Resources\\English Model V1\\audio_file_path.txt", $"\"{Application.StartupPath + "\\Resources\\English Model V2\\tortoise\\voices\\actual\\1.wav"}\"");
                    MessageBox.Show("Succesfully loaded your model!", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        catch
        {
            MessageBox.Show("The specified model is not valid.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        Process.GetCurrentProcess().Kill();
    }
}