using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Text.Json; // Adicione a referência ao Newtonsoft.Json
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;
using System.Data;

namespace Terminal_Server
{
    public partial class MainWindow : Window
    {
        ConsoleContent dc = new ConsoleContent();

      //  int port = 1234;
      //  string connectionString = @";";
      //  private static readonly string EncryptionKey = "MyEncryptionKey123"; // Use uma chave segura

        string localIP = GetLocalIPAddress();
        string machineName = Environment.MachineName;

        public MainWindow()
        {
            InitializeComponent();
            
            run();
            Task.Delay(5000);
            LoadScheduled();

        }

        private async void LoadScheduled()
        {
            await Task.Run(() => LoadScheduledTasks());
        }

        private async void run()
        {
            Update_SQL_Machine_Servers(connectionString, localIP, port, "Online", machineName);
            await Task.Run(() => Listener_TCP(port));
        }

        private void Click_Mover_Janela(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                if (sender is MenuItem menuItem)
                {
                    menuItem.Header = " ⧠ ";
                }
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                if (sender is MenuItem menuItem)
                {
                    menuItem.Header = "  ";
                }
            }
        }

        private void Close_Window(object sender, RoutedEventArgs e)
        {
            Update_SQL_Machine_Servers(connectionString, localIP, port, "Offline", machineName);
            Close();
        }

   

        public void TextMessage(string textx)
        {
            if (!string.IsNullOrEmpty(textx))
            {
                Dispatcher.Invoke(() =>
                {
                    outputLog.AppendLine($"Message: {textx}");
                    outputLog.AppendLine(Environment.NewLine);
                    //Dispatcher.Invoke(() => dc.ConsoleOutput.Add(e.Data));

                    TextTerminal.Text += $"Message: {textx}";
                    TextTerminal.Text += Environment.NewLine;
                });
            }
        }

        public void ErrorMessage(string textx)
        {
            if (!string.IsNullOrEmpty(textx))
            {
                Dispatcher.Invoke(() =>
                {
                    outputLog.AppendLine($"Error: {textx}");
                    outputLog.AppendLine(Environment.NewLine);
                    //Dispatcher.Invoke(() => dc.ConsoleOutput.Add(e.Data));

                    TextTerminal.Text += $"Error: {textx}";
                    TextTerminal.Text += Environment.NewLine;
                });
            }
        }


        private void Process_OutputDataReceived(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Dispatcher.Invoke(() =>
                {
                    outputLog.AppendLine($"Message: {text}");
                    outputLog.AppendLine(Environment.NewLine);
                    //Dispatcher.Invoke(() => dc.ConsoleOutput.Add(e.Data));

                    TextTerminal.Text += $"Message: {text}";
                    TextTerminal.Text += Environment.NewLine;
                    //Scroller.ScrollToBottom();
                });
            }
        }

        private void Process_ErrorDataReceived(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {

                if (!text.Contains("Warning:"))
                {
                   
                    Dispatcher.Invoke(() =>
                    {
                        outputLog.AppendLine($"Error: {text}");
                        outputLog.AppendLine(Environment.NewLine);
                        

                        TextTerminal.Text += $"Error: {text}";
                        TextTerminal.Text += Environment.NewLine;
                       
                    });
                }
                else 
                {
                    Dispatcher.Invoke(() =>
                    {
                        outputLog.AppendLine($"Warning: {text}");
                        outputLog.AppendLine(Environment.NewLine);
                        TextTerminal.Text += $"Warning: {text}";
                        TextTerminal.Text += Environment.NewLine;
                        
                    });
                }
            }
        }

        

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        


        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Nenhum endereço IPv4 encontrado na rede.");
        }

        public void Update_SQL_Machine_Servers(string connectionString, string localIP, int port, string serverStatus, string machineName)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM Servers WHERE IP = @IP";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@IP", localIP);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                    {
                        Updade_Computer_Servers(connection, serverStatus, localIP);
                    }
                    else
                    {
                        Insert_Computer_Servers(connection, serverStatus, localIP, port, machineName);
                    }
                    Dispatcher.Invoke(() => TextMessage("Informações inseridas com sucesso na tabela 'Servers'."));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => ErrorMessage("Ocorreu um erro: " + ex.Message));
                }
            }
        }

        public void Updade_Computer_Servers(SqlConnection connection, string serverStatus, string localIP)
        {
            string updateQuery = "UPDATE Servers SET Status = @Status WHERE IP = @IP";
            SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@Status", serverStatus);
            updateCommand.Parameters.AddWithValue("@IP", localIP);
            updateCommand.ExecuteNonQuery();
        }

        public void Insert_Computer_Servers(SqlConnection connection, string serverStatus, string localIP, int port, string machineName)
        {
            string insertQuery = @"
            INSERT INTO Servers (IP, Port, Status, MachineName) 
            VALUES (@IP, @Port, @Status, @MachineName)";

            SqlCommand command = new SqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@IP", localIP);
            command.Parameters.AddWithValue("@Port", port);
            command.Parameters.AddWithValue("@Status", serverStatus);
            command.Parameters.AddWithValue("@MachineName", machineName);
            command.ExecuteNonQuery();
        }

        public void Listener_TCP(int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            try
            {
                listener.Start();
                Dispatcher.Invoke(() => TextMessage($"Servidor escutando na porta {port}..."));
                while (true)
                {
                    try
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Dispatcher.Invoke(() => TextMessage("Cliente conectado!"));

                        NetworkStream stream = client.GetStream();
                        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                        string? command = reader.ReadLine();
                        Dispatcher.Invoke(() => TextMessage($"Comando recebido: {command}"));

                        if (command == "download")
                        {
                            string folderPath = reader.ReadLine() ?? string.Empty; 
                            Dispatcher.Invoke(() => TextMessage($"Baixando pasta: {folderPath}"));
                            SendFolderContents(folderPath, writer);
                        }
                        else
                        {
                            if (command != null)
                            {
                                string resultado = ProcessCommand(command);
                                writer.WriteLine("Comando processado com Sucesso!");
                                writer.WriteLine(resultado);
                                writer.WriteLine("END"); // Delimitador de fim de mensagem
                            }
                        }

                        client.Close();
                        Dispatcher.Invoke(() => TextMessage("Cliente desconectado!"));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => ErrorMessage($"Erro ao lidar com cliente: {ex.Message}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ErrorMessage($"Erro: {ex.Message}"));
            }
            finally
            {
                listener.Stop();
            }
        }


        private string ProcessCommand(string command)
        {
            Process processo = new Process();

            processo.StartInfo.FileName = "powershell.exe";
            processo.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy unrestricted -Command \"{command}\"";
            processo.StartInfo.UseShellExecute = false;
            processo.StartInfo.RedirectStandardOutput = true;
            processo.StartInfo.CreateNoWindow = true;

            processo.Start();
            string resultadoPowerShell = processo.StandardOutput.ReadToEnd();
            processo.WaitForExit();

            Dispatcher.Invoke(() => TextMessage("Resultado do comando PowerShell:"));
            Dispatcher.Invoke(() => TextMessage(resultadoPowerShell));
            return resultadoPowerShell;
        }


        private void SendFolderContents(string folderPath, StreamWriter writer)
        {
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    writer.WriteLine($"File:{System.IO.Path.GetFileName(file)}");
                    writer.WriteLine(Convert.ToBase64String(File.ReadAllBytes(file)));
                }

                foreach (string directory in Directory.GetDirectories(folderPath))
                {
                    writer.WriteLine($"Directory:{System.IO.Path.GetFileName(directory)}");
                    SendFolderContents(directory, writer);
                }
            }
        }

        public void LoadScheduledTasks()
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string parentDirPath = Directory.GetParent(appPath).FullName;
            string executionDirectory = System.IO.Path.Combine(parentDirPath, "execution");

            
                Directory.CreateDirectory(executionDirectory);
            

            while (true)
            //for (int i = 0;i < 5; i++)
            {
             //   TextSeparate();
             //   TextMessage("Initial Load Scheduler");
                string? name = null;               
                string? workqueue = null;
                string? pathprocess = null;

                byte[]? fileContent;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    try
                    {
                        connection.Open();
                        string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
                        string currentTime = DateTime.Now.ToString("HH:mm");

                        string query = @"
                SELECT TaskID, TaskName, TaskTime, Dates, Workqueues, Projects, ProjectsID, Server, Objects, ObjectsID
                FROM ScheduledTasks
                WHERE Server = @Server";

                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.AddWithValue("@Server", $"{machineName} ({localIP})");

                        SqlDataReader reader = command.ExecuteReader();

                        dc.ScheduledTasks.Clear();
                        while (reader.Read()) 
                        {
                            string datesJson = reader.GetString(3);
                            var datesArray = datesJson
                                .Split(',')
                                .Select(date => DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date)
                                .ToArray();

                            string taskTime = reader.GetString(2);

                            if (datesArray.Contains(DateTime.Now.Date) && taskTime == DateTime.Now.ToString("HH:mm:ss"))
                            {
                                if (!Directory.Exists(executionDirectory))
                                {
                                    Directory.CreateDirectory(executionDirectory);
                                }
                                else
                                {
                                    // Deleta todos os arquivos na pasta
                                    string[] files = Directory.GetFiles(executionDirectory);
                                    foreach (string file in files)
                                    {
                                        File.Delete(file);
                                    }

                                    // Deleta todas as subpastas e seus conteúdos
                                    string[] directories = Directory.GetDirectories(executionDirectory);
                                    foreach (string directory in directories)
                                    {
                                        Directory.Delete(directory, true);
                                    }
                                }

                                var task = new ScheduledTask
                                {
                                    TaskID = reader.GetInt32(0),
                                    TaskName = reader.GetString(1),
                                    TaskTime = taskTime,
                                    Dates = datesJson,
                                    Workqueue = reader.GetString(4) + "_Queue",
                                    Project = reader.GetString(5),
                                    ProjectID = reader.GetInt32(6).ToString(),
                                    Server = reader.GetString(7),
                                    Objects = reader.GetString(8),
                                    ObjectsID = reader.GetString(9)
                                };

                              //  dc.ScheduledTasks.Add(task);
                                // ===============================
                                workqueue = reader.GetString(4);
                                name = reader.GetString(7) + "_" + taskTime + "_" + DateTime.Now.Date;
                                // Download and save files listed in ObjectsID
                                string[] objectFileIds = task.ObjectsID.Split(',');
                                string[] objectFiles = task.Objects.Split(',');
                                int cont = 0;
                                foreach (string fileIdString in objectFileIds)
                                {
                                    fileContent = ReadFileContent(fileIdString.Trim());
                                    if (fileContent != null)
                                    {
                                        SaveFileToExecutionFolder(fileContent, objectFiles[cont]);
                                        Dispatcher.Invoke(() => TextMessage($"Arquivo {objectFiles[cont]} salvo na pasta de execução."));
                                       while (!File.Exists(executionDirectory +"\\"+ objectFiles[cont]))
                                    {
                                        Task.Delay(1000);
                                    }

                                    }
                                    cont++;
                                }

                                // Handle project files
                                fileContent = ReadFileContent(task.ProjectID.Trim());
                                if (fileContent != null)
                                {
                                    pathprocess = SaveFileToExecutionFolder(fileContent, task.Project.Trim(), workqueue +"_Queue"); //remover Replace(" ","_")

                                Dispatcher.Invoke(() => TextMessage($"Arquivo {task.Project.Trim()} salvo na pasta de execução."));

                                while (true)
                                {
                                    if(!File.Exists(executionDirectory + "\\" + task.Project.Trim()))
                                    {
                                        Task.Delay(1000);
                                    }
                                    else
                                    {
                                        runpython(name, pathprocess, workqueue);
                                        break;
                                    }
                                    
                                }

                            
                                
                                }
                            
                            }
                        }

                        
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => TextMessage($"Erro ao carregar tarefas agendadas: {ex.Message}"));
                    }
                }
                
                
                
            }
        }



        private string SaveFileToExecutionFolder(byte[] fileContent, string fileName, string? workqueue = null)

        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string parentDirPath = Directory.GetParent(appPath)?.FullName ?? string.Empty;
            string executionDirectory = Path.Combine(parentDirPath, "execution");

            if (!Directory.Exists(executionDirectory))
            {
                Directory.CreateDirectory(executionDirectory);
            }

            string filePath = Path.Combine(executionDirectory, fileName);
            File.WriteAllBytes(filePath, fileContent);

            if (workqueue != null)
            {
                ProcessPlaceholdersInFile(filePath, workqueue);
            }

            else
            {
                ProcessPlaceholdersInFile(filePath);
            }
            

            return filePath;
        }
        private byte[]? ReadFileContent(string fileId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Content FROM Files WHERE FileID = @FileID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FileID", fileId);

                // Ler o conteúdo do arquivo
                object result = command.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    return (byte[])result;
                }
                else
                {
                    return null;
                }
            }
        }        
        private void ProcessPlaceholdersInFile(string filePath, string? workqueue = null)
        {
           
            string fileContent = File.ReadAllText(filePath);
            string pattern = @"\{=(.*?)=\}"; // Regex para encontrar placeholders

            MatchCollection matches = Regex.Matches(fileContent, pattern);

            foreach (Match match in matches)
            {
                string credentialName = match.Groups[1].Value;
                var credentials = GetCredentials(credentialName);

                if (credentials.HasValue)
                {
                    var (username, password) = credentials.Value; // Desestruturando a tupla
                    string replacementText = $"{username}:{password}";
                    fileContent = fileContent.Replace(match.Value, replacementText);
                    
                }
            }

            string originalConnectionString = connectionString;
            string PythonconnectionString = originalConnectionString
            .Replace("Server=", "SERVER=")
            .Replace("\\", "\\\\")
            .Replace("User Id=", "UID=")
            .Replace("Password=", "PWD=")
            .Insert(0, "DRIVER={ODBC Driver 17 for SQL Server};");
            fileContent = fileContent.Replace("{_ConnectionString_}", PythonconnectionString);
            if (workqueue != null)
            {
                fileContent = fileContent.Replace("{_idprocess_}", currentIDProcess(workqueue).ToString());
            }
            
            File.WriteAllText(filePath, fileContent);
        }

        private int currentIDProcess(string workqueue)
        {
            
            string getLastIdQuery = $"SELECT IDENT_CURRENT('[{workqueue}]')";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(getLastIdQuery, connection);
                connection.Open();
                object result = command.ExecuteScalar();
                int lastId = Convert.ToInt32(result);
                return lastId + 1;
            }
        }

        private (string Username, string Password)? GetCredentials(string credentialName)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Username, Password FROM Credentials WHERE CredentialName = @CredentialName";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CredentialName", credentialName);

                SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    string username = reader.GetString(0);
                    string password = reader.GetString(1);
                    password = Decrypt(password);
                    return (username, password);
                }
            }

            return null;
        }

        private static string Decrypt(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                var keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
                Array.Resize(ref keyBytes, 16);
                aes.Key = keyBytes;
                aes.IV = new byte[16];

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        private StringBuilder outputLog = new StringBuilder();
        private async void runpython(string nameprocess, string filename, string workqueue)
        {

            InsertLogEntry(workqueue, nameprocess, "in process", DateTime.Now);
            int id = currentIDProcess(workqueue+"_Queue") -1 ;
            try
            {
                Process processo = new Process();
                processo.StartInfo.FileName = "python";
                processo.StartInfo.Arguments = @$"""{filename}"""; // remover Replace(" ","_")
                processo.StartInfo.RedirectStandardOutput = true;
                processo.StartInfo.RedirectStandardError = true;
                processo.StartInfo.UseShellExecute = false;
                processo.StartInfo.CreateNoWindow = true;
                processo.StartInfo.StandardOutputEncoding = Encoding.ASCII;
                processo.StartInfo.StandardErrorEncoding = Encoding.ASCII;
                //     processo.OutputDataReceived += Process_OutputDataReceived;
                //     processo.ErrorDataReceived += Process_ErrorDataReceived;

                //     processo.Warning += Process_OutputDataReceived;
               
                processo.Start();
                string errorss = processo.StandardError.ReadToEnd();
                string outmessage = processo.StandardOutput.ReadToEnd();
           //     processo.BeginOutputReadLine();
           //     processo.BeginErrorReadLine();
                

                await processo.WaitForExitAsync();  // Aguarda o término do processo de forma assíncrona

                // Após o processo terminar, você já terá capturado toda a saída e erro nos eventos
                if (!string.IsNullOrEmpty(errorss))  // Verifica se houve algum erro no log combinado
                {
                    Dispatcher.Invoke(() => Process_OutputDataReceived(outmessage));
                    Dispatcher.Invoke(() => Process_ErrorDataReceived(errorss));
                    updateLogEntry(id.ToString(),workqueue, nameprocess, "Terminated", DateTime.Now, outputLog.ToString());
                    Dispatcher.Invoke(() => TextMessage("Saved Log in Workqueue"));                    
                    //     Dispatcher.Invoke(() => ErrorMessage(outputLog.ToString()));
                }
                else
                {
                    Dispatcher.Invoke(() => Process_OutputDataReceived(outmessage));
                    updateLogEntry(id.ToString(), workqueue, nameprocess, "Completed", DateTime.Now, outputLog.ToString());
                    Dispatcher.Invoke(() => TextMessage("Saved Log in Workqueue"));
                    Dispatcher.Invoke(() => TextMessage(nameprocess + ": Completed"));
                }
            }
            catch (Exception ex)
            {
                updateLogEntry(id.ToString(), workqueue, nameprocess, "Terminated", DateTime.Now, outputLog.ToString() + ex.Message);
                Dispatcher.Invoke(() => TextMessage("Saved Log in Workqueue"));
                Dispatcher.Invoke(() => ErrorMessage(ex.Message));
            }
            
        }

        private void InsertLogEntry(string workqueue, string name, string status, DateTime time)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"INSERT INTO [{workqueue}_Queue] (Name, Status, Time) VALUES (@Name, @Status, @Time)";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Time", time);
                
                command.ExecuteNonQuery();
            }
        }

        private void updateLogEntry(string id,string workqueue, string name, string status, DateTime time, string logDescription)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"UPDATE [{workqueue}_Queue] SET Name = @Name, Status = @Status, Time = @Time, LogDescription = @LogDescription WHERE ID = @ID";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", id);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Time", time);

                if (logDescription != null)
                {
                    byte[] logDescriptionBytes = Encoding.UTF8.GetBytes(logDescription);
                    command.Parameters.Add("@LogDescription", SqlDbType.VarBinary).Value = logDescriptionBytes;
                }
                else
                {
                    command.Parameters.Add("@LogDescription", SqlDbType.VarBinary).Value = DBNull.Value;
                }
                command.ExecuteNonQuery();
            }
        }

    }    

    public class ScheduledTask
        {
            public int TaskID { get; set; }
            public string? TaskName { get; set; }
            public string? TaskTime { get; set; }
            public string? Dates { get; set; }
            public string? Workqueue { get; set; }
            public string? Project { get; set; }
            public string? ProjectID { get; set; }
            public string? Server { get; set; }
            public string? Objects { get; set; }
            public string? ObjectsID { get; set; }
        }

    public class ConsoleContent : INotifyPropertyChanged
    {
        string consoleInput = string.Empty;
        ObservableCollection<string> consoleOutput = new ObservableCollection<string>();
        ObservableCollection<ScheduledTask> scheduledTasks = new ObservableCollection<ScheduledTask>();

        public string ConsoleInput
        {
            get => consoleInput;
            set
            {
                consoleInput = value;
                OnPropertyChanged("ConsoleInput");
            }
        }

        public ObservableCollection<string> ConsoleOutput
        {
            get => consoleOutput;
            set
            {
                consoleOutput = value;
                OnPropertyChanged("ConsoleOutput");
            }
        }

        public ObservableCollection<ScheduledTask> ScheduledTasks
        {
            get => scheduledTasks;
            set
            {
                scheduledTasks = value;
                OnPropertyChanged("ScheduledTasks");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}


