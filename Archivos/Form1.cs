using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace Archivos
{
    public partial class Form1 : Form
    {
        string carpeta = @"C:\Users\cerva\source\repos\Archivos\Archivos\bin\Debug\Archivos";
        string archivoActual = "";

        List<Registro> lista = new List<Registro>();

        public Form1()
        {
            InitializeComponent();
            ConfigurarDataGridView();
            CargarListaArchivos();
            btnAgregarDesdeArchivo.Click += btnAgregarDesdeArchivo_Click;


            txtNombre.KeyDown += TextBox_KeyDown;
            txtEdad.KeyDown += TextBox_KeyDown;
            txtCarrera.KeyDown += TextBox_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CargarListaArchivos();
        }

        void ConfigurarDataGridView()
        {
            dataGridViewPersonas.Columns.Clear();
            dataGridViewPersonas.Columns.Add("Nombre", "Nombre");
            dataGridViewPersonas.Columns.Add("Edad", "Edad");
            dataGridViewPersonas.Columns.Add("Carrera", "Carrera");

            dataGridViewPersonas.AllowUserToAddRows = false;
            dataGridViewPersonas.ReadOnly = true;
        }

        private void btnCargar_Click(object sender, EventArgs e)
        {
            CargarListaArchivos();
            MessageBox.Show("Lista de archivos actualizada.");
        }

        void CargarListaArchivos()
        {
            comboBoxArchivos.Items.Clear();

            if (!Directory.Exists(carpeta))
            {
                MessageBox.Show("La carpeta no existe.");
                return;
            }

            var archivos = Directory.GetFiles(carpeta)
                .Where(f => f.EndsWith(".json") || f.EndsWith(".txt") || f.EndsWith(".xlsx"));

            foreach (string archivo in archivos)
            {
                string nombre = Path.GetFileName(archivo);
                comboBoxArchivos.Items.Add(nombre);
            }
        }

        private void comboBoxArchivos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxArchivos.SelectedItem == null) return;

            string nombreArchivo = comboBoxArchivos.SelectedItem.ToString();
            archivoActual = Path.Combine(carpeta, nombreArchivo);
            lista.Clear();

            if (archivoActual.EndsWith(".json"))
                CargarDesdeJson();
            else if (archivoActual.EndsWith(".txt"))
                CargarDesdeTxt();
            else if (archivoActual.EndsWith(".xlsx"))
                CargarDesdeExcel();

            MostrarPersonasEnDataGridView();
        }

        void CargarDesdeJson()
        {
            try
            {
                string texto = File.ReadAllText(archivoActual);
                var opciones = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var datos = JsonSerializer.Deserialize<List<Registro>>(texto, opciones);

                if (datos == null)
                {
                    MessageBox.Show("El archivo JSON no contiene datos válidos.");
                    return;
                }

                lista = datos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer el archivo JSON:\n{ex.Message}");
            }
        }

        void CargarDesdeTxt()
        {
            string[] lineas = File.ReadAllLines(archivoActual);
            foreach (var linea in lineas)
            {
                string[] partes = linea.Split(',');
                if (partes.Length == 3)
                    lista.Add(new Registro(partes[0], int.Parse(partes[1]), partes[2]));
            }
        }

        void CargarDesdeExcel()
        {
            var libro = new XLWorkbook(archivoActual);
            var hoja = libro.Worksheet(1);
            int fila = 2;

            while (!hoja.Cell(fila, 1).IsEmpty())
            {
                string nombre = hoja.Cell(fila, 1).GetString();
                int edad = int.Parse(hoja.Cell(fila, 2).GetString());
                string carrera = hoja.Cell(fila, 3).GetString();
                lista.Add(new Registro(nombre, edad, carrera));
                fila++;
            }
        }

        void MostrarPersonasEnDataGridView()
        {
            dataGridViewPersonas.Rows.Clear();

            foreach (var reg in lista)
            {
                dataGridViewPersonas.Rows.Add(reg.Nombre, reg.Edad, reg.Carrera);
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            AgregarRegistro();
        }

        void AgregarRegistro()
        {
            string nombre = txtNombre.Text.Trim();
            string edadTexto = txtEdad.Text.Trim();
            string carrera = txtCarrera.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(edadTexto) || string.IsNullOrEmpty(carrera))
            {
                MessageBox.Show("Faltan datos.");
                return;
            }

            if (!int.TryParse(edadTexto, out int edad))
            {
                MessageBox.Show("Edad inválida.");
                return;
            }

            var nuevoRegistro = new Registro(nombre, edad, carrera);
            lista.Add(nuevoRegistro);
            dataGridViewPersonas.Rows.Add(nombre, edad, carrera);

            MessageBox.Show("Registro agregado.");

            txtNombre.Clear();
            txtEdad.Clear();
            txtCarrera.Clear();
            txtNombre.Focus();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AgregarRegistro();
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string buscar = txtNombre.Text.ToLower();
            var resultado = lista.FirstOrDefault(r => r.Nombre.ToLower().Contains(buscar));

            if (resultado != null)
                MessageBox.Show($"Encontrado: {resultado.Nombre}, {resultado.Edad}, {resultado.Carrera}");
            else
                MessageBox.Show("No encontrado.");
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(archivoActual))
            {
                MessageBox.Show("Seleccione un archivo.");
                return;
            }

            if (archivoActual.EndsWith(".json"))
            {
                string json = JsonSerializer.Serialize(lista);
                File.WriteAllText(archivoActual, json);
            }
            else if (archivoActual.EndsWith(".txt"))
            {
                var lineas = lista.Select(r => $"{r.Nombre},{r.Edad},{r.Carrera}");
                File.WriteAllLines(archivoActual, lineas);
            }
            else if (archivoActual.EndsWith(".xlsx"))
            {
                var libro = new XLWorkbook();
                var hoja = libro.Worksheets.Add("Datos");

                hoja.Cell(1, 1).Value = "Nombre";
                hoja.Cell(1, 2).Value = "Edad";
                hoja.Cell(1, 3).Value = "Carrera";

                int fila = 2;
                foreach (var reg in lista)
                {
                    hoja.Cell(fila, 1).Value = reg.Nombre;
                    hoja.Cell(fila, 2).Value = reg.Edad;
                    hoja.Cell(fila, 3).Value = reg.Carrera;
                    fila++;
                }

                libro.SaveAs(archivoActual);
            }

            MessageBox.Show("Archivo guardado.");
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(archivoActual))
            {
                MessageBox.Show("Seleccione un archivo.");
                return;
            }

            if (archivoActual.EndsWith(".txt"))
            {
                MessageBox.Show("No se puede exportar un archivo de texto.");
                return;
            }

            if (archivoActual.EndsWith(".json"))
            {
                string nuevo = archivoActual.Replace(".json", "_exportado.xlsx");
                var libro = new XLWorkbook();
                var hoja = libro.Worksheets.Add("Datos");

                hoja.Cell(1, 1).Value = "Nombre";
                hoja.Cell(1, 2).Value = "Edad";
                hoja.Cell(1, 3).Value = "Carrera";

                int fila = 2;
                foreach (var reg in lista)
                {
                    hoja.Cell(fila, 1).Value = reg.Nombre;
                    hoja.Cell(fila, 2).Value = reg.Edad;
                    hoja.Cell(fila, 3).Value = reg.Carrera;
                    fila++;
                }

                libro.SaveAs(nuevo);
                MessageBox.Show("Exportado a Excel.");
            }
            else if (archivoActual.EndsWith(".xlsx"))
            {
                string nuevo = archivoActual.Replace(".xlsx", "_exportado.json");
                string json = JsonSerializer.Serialize(lista);
                File.WriteAllText(nuevo, json);
                MessageBox.Show("Exportado a JSON.");
            }
        }

        private void btnAgregarDesdeArchivo_Click(object sender, EventArgs e)
        {
            if (comboBoxArchivos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un archivo primero.");
                return;
            }
             if (archivoActual.EndsWith(".json")) CargarDesdeJson();
            else if (archivoActual.EndsWith(".txt")) CargarDesdeTxt();
            else if (archivoActual.EndsWith(".xlsx")) CargarDesdeExcel();

            // Toma el nombre del archivo seleccionado y construye la ruta completa
            string carpeta = @"C:\Users\cerva\source\repos\Archivos\Archivos\bin\Debug\Archivos";
            string nombreArchivo = comboBoxArchivos.SelectedItem.ToString();
            archivoActual = Path.Combine(carpeta, nombreArchivo);

            // Limpia la lista antes de agregar nuevos datos
            lista.Clear();

            // Carga según el tipo de archivo
            if (archivoActual.EndsWith(".json"))
                CargarDesdeJson();
            else if (archivoActual.EndsWith(".txt"))
                CargarDesdeTxt();
            else if (archivoActual.EndsWith(".xlsx"))
                CargarDesdeExcel();

            // Muestra los registros en el DataGridView
            MostrarPersonasEnDataGridView();
            MessageBox.Show("Datos del archivo agregados a la lista.");
        }
    }

    public class Registro
    {
        public string Nombre { get; set; }
        public int Edad { get; set; }
        public string Carrera { get; set; }

        public Registro() { }

        public Registro(string nombre, int edad, string carrera)
        {
            Nombre = nombre;
            Edad = edad;
            Carrera = carrera;
        }
    }
}
