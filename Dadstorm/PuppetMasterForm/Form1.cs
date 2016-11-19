using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace Dadstorm
{
    public partial class Form1 : Form
    {

        private PuppetMaster puppetMaster;

        public Form1()
        {
            InitializeComponent();
            puppetMaster = new PuppetMaster(this, new DelPrint(PrintToOutputBox));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public delegate void DelPrint(string m);

        public void PrintToOutputBox(string toPrint)
        {
            textBox1.AppendText(toPrint + "\r\n");

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            //Este path e apenas para testes tera de ser alterado
            //puppetMaster.StartProcessesPhase("C:\Users\Utilizador\Dropbox\Personal\Meic A\1ºAno\1ºSemestre\DAD\proj\git\dadstorm\Exemplos\dadstorm.config");
            puppetMaster.StartProcessesPhase(textBox9.Text);
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            puppetMaster.Start(textBox2.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String s = textBox5.Text;
            String[] toDivide = s.Split(' ');
            if (toDivide.Length == 2)
            {
                puppetMaster.Interval(toDivide[0], toDivide[1]);
            }
            else
            {
                //TODO: Exception PERGUNTAR
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            puppetMaster.Status();
        }


        private void button4_Click(object sender, EventArgs e)
        {
            string[] splitedLine = textBox3.Text.Split(' ');
            puppetMaster.Crash(splitedLine[1], splitedLine[2]);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string[] splitedLine = textBox8.Text.Split(' ');
            puppetMaster.Freeze(splitedLine[1], splitedLine[2]);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string[] splitedLine = textBox7.Text.Split(' ');
            puppetMaster.Unfreeze(splitedLine[1], splitedLine[2]);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            puppetMaster.Wait(textBox6.Text);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            puppetMaster.ProcessComands();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            puppetMaster.ProcessSingleCommand();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }


        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            // Confirm user wants to close
            switch (MessageBox.Show(this, "Are you sure you want to close?", "Closing", MessageBoxButtons.YesNo))
            {
                case DialogResult.No:
                    e.Cancel = true;
                    break;
                default:
                    puppetMaster.shutdownRep();
                    break;
            }
        }
    }
}
