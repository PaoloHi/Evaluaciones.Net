using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Data.SqlClient;
using static EvaluacionPaoloFernando.Service1;
using System.Web;

namespace EvaluacionPaoloFernando
{
    public partial class Service1 : ServiceBase
    {

        public class Ticket
        {
            public string IdTienda { get; set; }
            public string IdRegistradora { get; set; }
            public DateTime FechaHora { get; set; }
            public int NumeroTicket { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Total { get; set; }
        }

        private Timer timer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            timer = new Timer();
            timer.Interval = 60000; // 1 minuto (en milisegundos)
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Aquí se ejecutará el proceso cada minuto
            ProcesarArchivosFCT();
        }

        protected override void OnStop()
        {
            timer.Stop();
            timer.Dispose();
        }


        string rutaPendientes = @"C:\Users\pflores\Desktop\repo\Pendientes";
        string rutaProcesados = @"C:\Users\pflores\Desktop\repo\Procesados";
        string rutaLog = @"C:\Users\pflores\Desktop\repo\Log";
        string connectionString = "Server=34.218.6.36.1435\\evaluaciones; DATABASE=evaluacion_pflores; User ID=ECOMMERCEREAL_TEST1; Password=Megadeth18;Trusted_Connection=false; MultipleActiveResultSets=true;Integrated Security=true; TrustServerCertificate=false;";

        private void ProcesarArchivosFCT()
        {
                        
            string[] archivosPendientes = Directory.GetFiles(rutaPendientes, "*.fct");

            foreach (string archivo in archivosPendientes)
            {
                try
                {
                    // Leer el contenido del archivo *.fct y procesar los datos para obtener los valores del ticket
                    Ticket ticket = LeerArchivoFCT(archivo);

                    InsertarEnTickets(ticket, connectionString);

                    // Mover el archivo procesado a la carpeta "Procesados"
                    MoverArchivoProcesado(archivo, rutaProcesados);
                }
                catch (Exception ex)
                {
                    // Registrar el error en el archivo de log
                    RegistrarErrorEnLog(ex, rutaLog, archivo);

                    // Mover el archivo a la carpeta de errores (*.fct_error)
                    MoverArchivoError(archivo, rutaPendientes);

                    RenombrarArchivoError(archivo);
                }
            }
        }

        private void MoverArchivoProcesado(string rutaArchivo, string rutaDestino)
        {
            // Implementa el código para mover el archivo a la carpeta "Procesados"
            string nombreArchivo = Path.GetFileName(rutaArchivo);
            string nuevaRutaArchivo = Path.Combine(rutaDestino, nombreArchivo);
            File.Move(rutaArchivo, nuevaRutaArchivo);
        }

        private Ticket LeerArchivoFCT(string rutaArchivo)
        {
             Ticket ticket = new Ticket ();

            // Verificar si el archivo existe
            if (File.Exists(rutaArchivo))
            {
                // Leer todas las líneas del archivo
                string[] lineas = File.ReadAllLines(rutaArchivo);

                // Procesar las líneas del archivo
                foreach (string linea in lineas)
                {
                    // Dividir la línea en sus partes usando el caracter '|'
                    string[] valores = linea.Split('|');

                    // Verificar si la línea tiene el número correcto de valores
                    if (valores.Length == 6)
                    {
                        // Obtener los valores individuales
                         ticket.IdTienda= valores[0];
                         ticket.IdRegistradora = valores[1].ToString();
                        ticket.FechaHora = DateTime.Parse(valores[2]);
                        ticket.NumeroTicket = int.Parse(valores[3]);
                        ticket.Impuesto = decimal.Parse(valores[4]);
                        ticket.Total = decimal.Parse(valores[5]);

                        // Hacer lo que necesites con los datos del ticket
                        Console.WriteLine($"Id_Tienda: {ticket.IdTienda}");
                        Console.WriteLine($"Id_Registradora: {ticket.IdRegistradora}");
                        Console.WriteLine($"FechaHora: {ticket.FechaHora}");
                        Console.WriteLine($"NumeroTicket: {ticket.NumeroTicket}");
                        Console.WriteLine($"Impuesto: {ticket.Impuesto}");
                        Console.WriteLine($"Total: {ticket.Total}");
                    }
                    else
                    {
                        // La línea no tiene el formato esperado, puedes manejar el error aquí
                        Console.WriteLine($"Error: La línea no tiene el formato correcto: {linea}");
                    }
                }
            }
            else
            {
                // El archivo no existe
                Console.WriteLine("Error: El archivo no existe.");
            }

            return ticket;

        }

        private void InsertarEnTickets(Ticket ticket, string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Definir la consulta SQL para insertar el registro en la tabla "Tickets"
                    string query = "INSERT INTO Tickets (IdTienda, IdRegistradora, FechaHora, NumeroTicket, Impuesto, Total) " +
                                   "VALUES (@IdTienda, @IdRegistradora, @FechaHora, @NumeroTicket, @Impuesto, @Total)";

                    // Crear el objeto SqlCommand y agregar los parámetros
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdTienda", ticket.IdTienda);
                        command.Parameters.AddWithValue("@IdRegistradora", ticket.IdRegistradora);
                        command.Parameters.AddWithValue("@FechaHora", ticket.FechaHora);
                        command.Parameters.AddWithValue("@NumeroTicket", ticket.NumeroTicket);
                        command.Parameters.AddWithValue("@Impuesto", ticket.Impuesto);
                        command.Parameters.AddWithValue("@Total", ticket.Total);

                        // Ejecutar la consulta
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                   
                }
            }
        }


        private void RegistrarErrorEnLog(Exception ex, string rutaLog, string archivo)
        {
            // Implementa el código para registrar el error en un archivo de log.
            // Aquí un ejemplo sencillo usando StreamWriter:
            string logFile = Path.Combine(rutaLog, "Log.txt");
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.WriteLine($"{DateTime.Now}: Error procesando archivo {archivo}");
                writer.WriteLine($"Mensaje de error: {ex.Message}");
                // Puedes agregar más información del error si lo deseas.
            }
        }


        private void MoverArchivoError(string rutaArchivo, string rutaDestino)
        {
            // Implementa el código para mover el archivo a la carpeta de errores (*.fct_error)
            // Aquí un ejemplo sencillo usando System.IO:
            string nombreArchivo = Path.GetFileName(rutaArchivo);
            string nuevaRutaArchivo = Path.Combine(rutaDestino, nombreArchivo.Replace(".fct", ".fct_error"));
            File.Move(rutaArchivo, nuevaRutaArchivo);
        }

        private void RenombrarArchivoError(string rutaArchivo)
        {
            // Obtiene el nombre del archivo sin la extensión actual
            string nombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo);

            // Combina el nombre del archivo con la nueva extensión ".fct_error"
            string nuevoNombreArchivo = nombreArchivo + ".fct_error";

            // Obtiene la ruta del archivo con la nueva extensión
            string nuevaRutaArchivo = Path.Combine(Path.GetDirectoryName(rutaArchivo), nuevoNombreArchivo);

            // Renombra el archivo
            File.Move(rutaArchivo, nuevaRutaArchivo);
        }



    }
}
