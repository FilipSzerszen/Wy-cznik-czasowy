using System;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace Wyłącznik_czasowy
{
    public partial class Form1 : Form
    {
        bool pozycjonowanie = true;
        string port_com = "";
        public string Tekst;
        public static Form1 Form;
        byte[] Harmonogram = new byte[10];

        public Form1()
        {
            InitializeComponent();
            Form = this;
            zegar_text.Text = Wschod_zachod();
            //tb = tBoxTemp;
        }

        private void zazn_Click(object sender, EventArgs e)
        {
            var CheckBoxes = this.gBox1.Controls.OfType<CheckBox>();
            for (int i = 0; i < 48; i++)
            {
                var cb = CheckBoxes.Where(p => p.Name == "cb" + i).FirstOrDefault();
                if (cb != null) cb.Checked = true;
            }

        }

        string czas()
        {
            return DateTime.Now.ToString("T");
        }

        private void odzn_Click(object sender, EventArgs e)
        {
            var CheckBoxes = this.gBox1.Controls.OfType<CheckBox>();
            for (int i = 0; i < 48; i++)
            {
                var cb = CheckBoxes.Where(p => p.Name == "cb" + i).FirstOrDefault();
                if (cb != null) cb.Checked = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = czas() + "   " + Czas_pracy();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int n = 0;
            Tekst = "";
            var CheckBoxes = this.gBox1.Controls.OfType<CheckBox>();
            for (int i = 0; i < 48; i++)
            {
                var cb = CheckBoxes.Where(p => p.Name == "cb" + i).FirstOrDefault();
                if (cb != null && cb.Checked) Tekst += "1"; else Tekst += "0";
                if ((i + 1) % 8 == 0)
                {
                    Harmonogram[n] = Convert.ToByte(Tekst, 2);
                    n++;
                    Tekst = "";
                }
            }
            Harmonogram[6] = (byte)DateTime.Now.Hour;
            Harmonogram[7] = (byte)DateTime.Now.Minute;
            Harmonogram[8] = (byte)DateTime.Now.Second;
            Harmonogram[9] = 0;

            Zapisz_dane();
        }

        void Otworz_port()
        {
            try
            {
                serialPort1.DtrEnable = false;
                serialPort1.RtsEnable = false;
                serialPort1.PortName = cBoxPortCom.Text;
                serialPort1.BaudRate = 2400;
                serialPort1.DataBits = 8;
                serialPort1.StopBits = (StopBits)1;
                serialPort1.Parity = Parity.None;
                serialPort1.Open();
            }
            catch 
            {
                MessageBox.Show("Podłącz kabel i wybierz port komunikacyjny w prawym dolnym rogu...", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                rBtnON.Enabled = false;
                lblStatus.Text = "OFF";
            }
        }

        public void Zamknij_port()
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
        }

        public void Zapisz_dane()
        {
            byte dana;
            int m = 100;

            if (serialPort1.IsOpen == false)
            {
                Otworz_port();
            }
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(new byte[] { 0b00001010 }, 0, 1);

                tb.Text = "";
                for (int i = 0; i < 10; i++)
                {
                    serialPort1.Write(new byte[] { Harmonogram[i] }, 0, 1);
                    while (m > 0)
                    {
                        if (serialPort1.BytesToRead > 0)
                        {
                            m = 100;
                            dana = (byte)serialPort1.ReadByte();
                            if (dana != Harmonogram[i])
                            {
                                i--;
                                serialPort1.Write(new byte[] { 0b00000000 }, 0, 1);
                            }
                            else
                            {
                                serialPort1.Write(new byte[] { 0b00000001 }, 0, 1);
                            }
                            break;
                        }
                        System.Threading.Thread.Sleep(10);
                        m--;
                    }
                    if (m == 0)
                    {
                        MessageBox.Show("Błąd zapisu, spróbuj jeszcze raz...", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                Zamknij_port();
                MessageBox.Show("Zapis wykonany pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void Zgraj_dane()
        {
            byte dana = 0;
            byte czyok;
            int m;

            if (serialPort1.IsOpen == false)
            {
                Otworz_port();
            }
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(new byte[] { 0b11111111 }, 0, 1);
                Tekst = "";
                tb.Text = "";
                zegar_text.Text = "";
                for (int i = 0; i < 10; i++)
                {
                    m = 100;
                    while (m > 0)
                    {
                        if (serialPort1.BytesToRead > 0)
                        {
                            m = 100;
                            dana = (byte)serialPort1.ReadByte();
                            serialPort1.Write(new byte[] { dana }, 0, 1);
                            break;
                        }
                        System.Threading.Thread.Sleep(10);
                        m--;
                    }
                    m = 100;
                    while (m > 0)
                    {
                        if (serialPort1.BytesToRead > 0)
                        {
                            m = 100;
                            czyok = (byte)serialPort1.ReadByte();
                            if (czyok != 1)
                            {
                                i--;
                            }
                            else
                            {
                                tb.Text += dana.ToString() + "\r\n";
                                if (i < 6) Tekst += Convert.ToString(dana, 2).PadLeft(8, '0');
                                if (i == 6)
                                {
                                    if (dana < 10) zegar_text.Text += "Godzina odczytana z urządzenia: 0" + dana.ToString() + ":";
                                    else zegar_text.Text += "Godzina odczytana z urządzenia: " + dana.ToString() + ":";
                                };
                                if (i == 7)
                                {
                                    if (dana < 10) zegar_text.Text += "0" + dana.ToString() + ":";
                                    else zegar_text.Text += dana.ToString() + ":";
                                };
                                if (i == 8)
                                {
                                    if (dana < 10) zegar_text.Text += "0" + dana.ToString();
                                    else zegar_text.Text += dana.ToString();
                                };
                            }
                            break;
                        }
                        System.Threading.Thread.Sleep(10);
                        m--;
                    }
                    if (m == 0)
                    {
                        MessageBox.Show("Błąd odczytu, spróbuj jeszcze raz...", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                var CheckBoxes = this.gBox1.Controls.OfType<CheckBox>();
                for (int i = 0; i < 48; i++)
                {
                    var cb = CheckBoxes.Where(p => p.Name == "cb" + i).FirstOrDefault();

                    if (Tekst[i] == 48) cb.Checked = false; //jeśli "0" (reprezentacja asci w tablicy) wtedy nie zaznaczaj, p wprzeciwnym razie zaznacz
                    else cb.Checked = true;
                }
                Zamknij_port();
                MessageBox.Show("Odczyt wykonany pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Zgraj_dane();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                Otworz_port();
                if (serialPort1.IsOpen)
                {
                    port_com = cBoxPortCom.Text;
                    rBtnON.Enabled = true;
                    lblStatus.Text = "ON";
                }
            }
            Zamknij_port();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cBoxPortCom.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            cBoxPortCom.Items.AddRange(ports);
            if (pozycjonowanie == true)
            {
                Form.SetDesktopLocation((Screen.PrimaryScreen.Bounds.Width - Form.Size.Width) / 2, (Screen.PrimaryScreen.Bounds.Height - Form.Size.Height) / 2);
                pozycjonowanie = false;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            zegar_text.Text = Wschod_zachod();
        }

        #region Oblicznanie wschodu i zachodu słońca dla Wrocławia (instrukcja z internetu)
        string Wschod_zachod()
        {
            int temp;
            double wiek, a, b, c, d, f, g, h, rad, w, z; //z instrukcji obliczania z internetu

            wiek = 367 * Convert.ToInt32(DateTime.Now.Year.ToString());
            temp = 367 * Convert.ToInt32(DateTime.Now.Month.ToString());
            if (temp == 1 || temp == 2) temp = (7 * Convert.ToInt32(DateTime.Now.Year.ToString())) / 4; else temp = (7 * (Convert.ToInt32(DateTime.Now.Year.ToString()) + 1)) / 4;
            wiek -= temp;
            temp = (275 * Convert.ToInt32(DateTime.Now.Month.ToString())) / 9;
            wiek += temp + Convert.ToInt32(DateTime.Now.Day.ToString()) - 730531.5;
            wiek /= 36525;
            a = (4.8949504201433 + 628.331969753199 * wiek) % 6.28318530718;
            b = (6.2400408 + 628.3019501 * wiek) % 6.28318530718;
            c = 0.409093 - 0.0002269 * wiek;
            d = 0.033423 * Math.Sin(b) + 0.00034907 * Math.Sin(2 * b);
            f = 0.0430398 * Math.Sin(2 * (a + d)) - 0.00092502 * Math.Sin(4 * (a + d)) - d;
            g = Math.Asin(Math.Sin(c) * Math.Sin(a + d));
            rad = 3.14159265359 / 180;
            h = (Math.Sin(rad * (-0.833)) - Math.Sin(rad * 51.1) * Math.Sin(g)) / (Math.Cos(rad * 51.1) * Math.Cos(g));
            w = (3.14159265359 - (f + rad * 17.03333 + Math.Acos(h))) / (15 * rad);
            z = (3.14159265359 - (f + rad * 17.03333 - Math.Acos(h))) / (15 * rad);
            if ((Convert.ToInt32(DateTime.Now.Month.ToString()) == 3 && Convert.ToInt32(DateTime.Now.Day.ToString()) > 26) || (Convert.ToInt32(DateTime.Now.Month.ToString()) > 3 && Convert.ToInt32(DateTime.Now.Month.ToString()) < 10) || (Convert.ToInt32(DateTime.Now.Month.ToString()) == 10 && Convert.ToInt32(DateTime.Now.Day.ToString()) < 31))
            {
                w += 2;
                z += 2;
            }
            else
            {
                w += 1;
                z += 1;
            }
            return "Słońce wschodzi o " + (int)w + ":" + (int)((w % 1) * 60) + ", zachodzi o " + (int)z + ":" + (int)((z % 1) * 60);
        }
        #endregion
        string Czas_pracy()
        {
            int n = 0;
            double energia = 0;
            var CheckBoxes = this.gBox1.Controls.OfType<CheckBox>();
            for (int i = 0; i < 48; i++)
            {
                var cb = CheckBoxes.Where(p => p.Name == "cb" + i).FirstOrDefault();
                if (cb != null && cb.Checked) n++;
            }
            energia = ((48 - n) * 0.5 + n * 5.5) / 2;
            return "Teoretyczny czas pracy na pełnej baterii: " + (int)(4400 / energia) + "dni (" + energia + "mAh/dzień)";
        }

        private void MouseEnter(object sender, EventArgs e) { (sender as CheckBox).Checked ^= true; }

    }
}