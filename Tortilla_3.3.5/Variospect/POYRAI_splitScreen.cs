using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Variospect
{
    public partial class POYRAI_splitScreen : Form
    {
        public POYRAI_splitScreen()
        {
            InitializeComponent();

            timer1.Enabled = true; //timer nesnesini başlatıyoruz.
            progressBar1.Visible = true; //progressBar nesnesinin gözükmesini istiyorsanız bu değeri true yapabilirsiniz.
         
            //this.BackColor = Color.LimeGreen;
            //this.TransparencyKey = Color.LimeGreen;
            this.WindowState = FormWindowState.Maximized;
        }

        private void POYRAI_splitScreen_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value += 5;
            progressBar1.Refresh();
            if (progressBar1.Value == 100)
            {
                System.Threading.Thread.Sleep(100);
                timer1.Stop();
                this.Close();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
