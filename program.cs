/****************************************
    TEAMMEMBERS:
****************************************/
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace SpaceSimulationApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SimulationForm());
        }
    }
}
namespace SpaceSimulationApp
{
    public abstract class CelestialBody
    {
        public string Name { get; set; }
        public double Mass { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        protected CelestialBody(string name, double mass, double x, double y)
        {
            Name = name;
            Mass = mass;
            X = x;
            Y = y;
        }

        public abstract void UpdatePosition(double timeStep);
    }

    public class Satellite : CelestialBody
    {
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public SatelliteCategory Category { get; set; }
        public List<PointF> Trajectory { get; set; }
        public double AngularVelocity { get; set; }
        private double OrbitRadius { get; }
        private double Phase { get; }
        public double Speed { get; private set; }
        public double Angle { get; private set; }

        public Satellite(string name, double mass, double orbitRadius, double speed, double angle, double phase, SatelliteCategory category, double angularVelocity)
    : base(name, mass, 0, 0)
        {

            InitializeSatellite(orbitRadius, speed, angle, phase);
            Category = category;
            Trajectory = new List<PointF> { new PointF((float)X, (float)Y) };
            AngularVelocity = angularVelocity;
            OrbitRadius = orbitRadius;
            Phase = phase;
            Speed = speed;
            Angle = angle;
        }

        public override void UpdatePosition(double timeStep)
        {
            UpdateOrbitPosition(timeStep);
        }

        private void UpdateOrbitPosition(double timeStep)
        {
            double angle = Math.Atan2(Y, X);
            double newAngle = angle + AngularVelocity * timeStep;
            double newX = OrbitRadius * Math.Cos(newAngle);
            double newY = OrbitRadius * Math.Sin(newAngle);

            X = newX;
            Y = newY;
            Trajectory.Add(new PointF((float)X, (float)Y));
        }

        private void InitializeSatellite(double orbitRadius, double speed, double angle, double phase)
        {
            double radianAngle = Math.PI * angle / 180;
            double radianPhase = Math.PI * phase / 180;

            X = orbitRadius * Math.Cos(radianAngle);
            Y = orbitRadius * Math.Sin(radianAngle);

            VelocityX = -speed * Math.Sin(radianAngle);
            VelocityY = speed * Math.Cos(radianAngle);
        }


        public override string ToString()
        {
            return $"{Name}, {Mass}, {OrbitRadius}, {Speed}, {Angle}, {Phase}, {Category}, {AngularVelocity}";
        }

    }

    public class GroundStation : CelestialBody
    {
        public double DetectionRange { get; set; }
        public StationCategory Category { get; set; }

        public GroundStation(string name, double mass, double x, double y, double detectionRange, StationCategory category)
            : base(name, mass, x, y)
        {
            DetectionRange = detectionRange;
            Category = category;
        }

        public override void UpdatePosition(double timeStep)
        {
        }

        public bool CanDetect(Satellite satellite)
        {
            double distance = Math.Sqrt(Math.Pow(X - satellite.X, 2) + Math.Pow(Y - satellite.Y, 2));
            return distance <= DetectionRange;
        }
    }

    public enum SatelliteCategory
    {
        Receiver,
        Transmitter
    }

    public enum StationCategory
    {
        Communicating,
        Tracking,
        Both
    }
}
namespace SpaceSimulationApp
{
    public class SimulationEngine
    {
        public List<Satellite> Satellites { get; set; } = new List<Satellite>();
        public List<GroundStation> GroundStations { get; set; } = new List<GroundStation>();

        public void ExecuteSimulationStep(double timeStep)
        {
            foreach (var satellite in Satellites)
            {
                satellite.UpdatePosition(timeStep);
            }
            RecordCommunicationEvents();
        }

        private void RecordCommunicationEvents()
        {
            var logEntries = new List<string>();
            foreach (var satellite in Satellites)
            {
                foreach (var station in GroundStations)
                {
                    if (station.CanDetect(satellite))
                    {
                        string log = $"At time {DateTime.Now}, {satellite.Name} connects with {station.Name}";
                        Console.WriteLine(log);
                        logEntries.Add(log);
                    }
                }
            }
            SaveLogEntries(logEntries);
        }

        private void SaveLogEntries(List<string> logEntries)
        {
            try
            {
                File.AppendAllLines("communication_log.txt", logEntries);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing communication log: {ex.Message}");
            }
        }
    }
}



namespace SpaceSimulationApp
{
    public class SimulationForm : Form
    {
        private SimulationEngine simulationEngine;
        private System.Windows.Forms.Timer simulationTimer;
        private PlotView plotView;
        private double currentTime;
        private double timeStep = 0.1;
        private TextBox totalDurationInput;
        private TextBox timeStepInput;
        private Button saveButton;
        private Button loadButton;
        private ListBox objectListBox;
        private Button editButton;

#pragma warning disable CS8618
        public SimulationForm()
#pragma warning restore CS8618
        {
            SetupComponents();
            LoadSampleData();
            UpdateObjectList();
        }

        private void SetupComponents()
        {
            simulationEngine = new SimulationEngine();
            simulationTimer = new System.Windows.Forms.Timer { Interval = 100 };
#pragma warning disable CS8622
            simulationTimer.Tick += OnSimulationTick;
#pragma warning restore CS8622

            plotView = new PlotView { Dock = DockStyle.Fill };
            this.Controls.Add(plotView);

            var startButton = new Button { Text = "Start Simulation", Dock = DockStyle.Top };
#pragma warning disable CS8622
            startButton.Click += OnStartButtonClick;
#pragma warning restore CS8622
            this.Controls.Add(startButton);

            var stopButton = new Button { Text = "Stop Simulation", Dock = DockStyle.Top };
#pragma warning disable CS8622
            stopButton.Click += OnStopButtonClick;
#pragma warning restore CS8622
            this.Controls.Add(stopButton);

            saveButton = new Button { Text = "Save Simulation Data", Dock = DockStyle.Top };
#pragma warning disable CS8622
            saveButton.Click += OnSaveButtonClick;
#pragma warning restore CS8622
            this.Controls.Add(saveButton);

            loadButton = new Button { Text = "Load Simulation Data", Dock = DockStyle.Top };
#pragma warning disable CS8622
            loadButton.Click += OnLoadButtonClick;
#pragma warning restore CS8622
            this.Controls.Add(loadButton);

            var totalDurationLabel = new Label { Text = "Total Simulation Duration (s):", Dock = DockStyle.Top };
            this.Controls.Add(totalDurationLabel);

            totalDurationInput = new TextBox { Text = "1000", Dock = DockStyle.Top };
            this.Controls.Add(totalDurationInput);

            var timeStepLabel = new Label { Text = "Time Step (s):", Dock = DockStyle.Top };
            this.Controls.Add(timeStepLabel);

            timeStepInput = new TextBox { Text = "0.1", Dock = DockStyle.Top };
            this.Controls.Add(timeStepInput);

            objectListBox = new ListBox { Dock = DockStyle.Left, Width = 200 };
            this.Controls.Add(objectListBox);

            editButton = new Button { Text = "Edit Selected Object", Dock = DockStyle.Top };
#pragma warning disable CS8622
            editButton.Click += OnEditButtonClick;
#pragma warning restore CS8622
            this.Controls.Add(editButton);
        }

        private void LoadSampleData()
        {
            simulationEngine.Satellites.Add(new Satellite("Satellite1", 1000, 7000000, 1000, 90, 0, SatelliteCategory.Receiver, 0.001));
            simulationEngine.Satellites.Add(new Satellite("Satellite2", 1500, 8000000, 1000, 90, 0, SatelliteCategory.Transmitter, 0.001));
            simulationEngine.GroundStations.Add(new GroundStation("Station1", 500, 0, 0, 10000, StationCategory.Communicating));
            simulationEngine.GroundStations.Add(new GroundStation("Station2", 600, 0, 0, 45, StationCategory.Tracking));
            simulationEngine.GroundStations.Add(new GroundStation("Station3", 700, 0, 0, 5000, StationCategory.Both));
        }

        private void UpdateObjectList()
        {
            objectListBox.Items.Clear();
            foreach (var satellite in simulationEngine.Satellites)
            {
                objectListBox.Items.Add(satellite.Name);
            }
            foreach (var station in simulationEngine.GroundStations)
            {
                objectListBox.Items.Add(station.Name);
            }
        }

        private void OnStartButtonClick(object sender, EventArgs e)
        {
            currentTime = 0;
            if (double.TryParse(totalDurationInput.Text, out double total) && double.TryParse(timeStepInput.Text, out double step))
            {
                timeStep = step;
                simulationTimer.Start();
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for total duration and time step.");
            }
        }

        private void OnStopButtonClick(object sender, EventArgs e)
        {
            simulationTimer.Stop();
        }

        private void OnSimulationTick(object sender, EventArgs e)
        {
            simulationEngine.ExecuteSimulationStep(timeStep);
            currentTime += timeStep;
            UpdatePlot();
            if (currentTime >= double.Parse(totalDurationInput.Text))
            {
                simulationTimer.Stop();
            }
        }

        private void UpdatePlot()
        {
            var model = new PlotModel { Title = "Satellite Trajectories" };

            foreach (var satellite in simulationEngine.Satellites)
            {
                var trajectorySeries = new LineSeries { Title = satellite.Name };
                trajectorySeries.Points.AddRange(satellite.Trajectory.Select(p => new DataPoint(p.X, p.Y)));
                model.Series.Add(trajectorySeries);
            }

            foreach (var station in simulationEngine.GroundStations)
            {
                var stationSeries = new ScatterSeries { MarkerType = MarkerType.Circle, MarkerSize = 4, Title = station.Name };
                stationSeries.Points.Add(new ScatterPoint(station.X, station.Y));
                model.Series.Add(stationSeries);
            }

            plotView.Model = model;
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files|*.txt|All Files|*.*",
                Title = "Save Simulation Data"
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        writer.WriteLine("Satellite:");
                        foreach (var satellite in simulationEngine.Satellites)
                        {
                            writer.WriteLine(satellite.ToString());
                        }

                        writer.WriteLine("Ground Station:");
                        foreach (var station in simulationEngine.GroundStations)
                        {
                            writer.WriteLine(station.ToString());
                        }

                        MessageBox.Show("Simulation data saved successfully.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving simulation data: {ex.Message}");
                }
            }
        }

        private void OnLoadButtonClick(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files|*.txt|All Files|*.*",
                Title = "Load Simulation Data"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var reader = new StreamReader(openFileDialog.FileName))
                    {
                        simulationEngine = new SimulationEngine();
                        string line;
#pragma warning disable CS8600
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Satellite:"))
                            {
                                string[] parts = line.Split(':');
                                string[] satelliteData = parts[1].Split(',');
                                string name = satelliteData[0].Trim();
                                double mass = double.Parse(satelliteData[1].Trim());
                                double orbitRadius = double.Parse(satelliteData[2].Trim());
                                double speed = double.Parse(satelliteData[3].Trim());
                                double angle = double.Parse(satelliteData[4].Trim());
                                double phase = double.Parse(satelliteData[5].Trim());
                                SatelliteCategory category = (SatelliteCategory)Enum.Parse(typeof(SatelliteCategory), satelliteData[6].Trim());
                                double angularVelocity = double.Parse(satelliteData[7].Trim());
                                simulationEngine.Satellites.Add(new Satellite(name, mass, orbitRadius, speed, angle, phase, category, angularVelocity));
                            }
                        }
#pragma warning restore CS8600
                        UpdateObjectList();
                        MessageBox.Show("Simulation data loaded successfully.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading simulation data: {ex.Message}");
                }
            }
        }

        private void OnEditButtonClick(object sender, EventArgs e)
        {
            if (objectListBox.SelectedItem != null)
            {
#pragma warning disable CS8600
                string selectedName = objectListBox.SelectedItem.ToString();
#pragma warning restore CS8600
#pragma warning disable CS8600
                var selectedObject = simulationEngine.Satellites.FirstOrDefault(s => s.Name == selectedName)
                                     ?? (CelestialBody)simulationEngine.GroundStations.FirstOrDefault(g => g.Name == selectedName);
#pragma warning restore CS8600

                if (selectedObject != null)
                {
                    var editForm = new EditForm(selectedObject);
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        UpdateObjectList();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an object to edit.");
            }
        }
    }
}

namespace SpaceSimulationApp
{
    public class EditForm : Form
    {
        private CelestialBody celestialBody;
        private TextBox nameInput;
        private TextBox massInput;
        private TextBox xInput;
        private TextBox yInput;
        private TextBox additionalInput1;
        private TextBox additionalInput2;
        private Label additionalLabel1;
        private Label additionalLabel2;
        private Button saveButton;

#pragma warning disable CS8618
        public EditForm(CelestialBody celestialBody)
#pragma warning restore CS8618
        {
            this.celestialBody = celestialBody;
            InitializeComponents();
            PopulateFields();
        }

        private void InitializeComponents()
        {
            nameInput = new TextBox { Dock = DockStyle.Top };
            massInput = new TextBox { Dock = DockStyle.Top };
            xInput = new TextBox { Dock = DockStyle.Top };
            yInput = new TextBox { Dock = DockStyle.Top };

            additionalLabel1 = new Label { Dock = DockStyle.Top };
            additionalInput1 = new TextBox { Dock = DockStyle.Top };
            additionalLabel2 = new Label { Dock = DockStyle.Top };
            additionalInput2 = new TextBox { Dock = DockStyle.Top };

            saveButton = new Button { Text = "Save", Dock = DockStyle.Top };
#pragma warning disable CS8622
            saveButton.Click += OnSaveButtonClick;
#pragma warning restore CS8622

            this.Controls.Add(saveButton);
            this.Controls.Add(additionalInput2);
            this.Controls.Add(additionalLabel2);
            this.Controls.Add(additionalInput1);
            this.Controls.Add(additionalLabel1);
            this.Controls.Add(yInput);
            this.Controls.Add(xInput);
            this.Controls.Add(massInput);
            this.Controls.Add(nameInput);
        }

        private void PopulateFields()
        {
            nameInput.Text = celestialBody.Name;
            massInput.Text = celestialBody.Mass.ToString();
            xInput.Text = celestialBody.X.ToString();
            yInput.Text = celestialBody.Y.ToString();

            if (celestialBody is Satellite satellite)
            {
                additionalLabel1.Text = "Velocity X:";
                additionalInput1.Text = satellite.VelocityX.ToString();
                additionalLabel2.Text = "Velocity Y:";
                additionalInput2.Text = satellite.VelocityY.ToString();
            }
            else if (celestialBody is GroundStation groundStation)
            {
                additionalLabel1.Text = "Detection Range:";
                additionalInput1.Text = groundStation.DetectionRange.ToString();
                additionalLabel2.Text = "";
                additionalInput2.Visible = false;
            }
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            celestialBody.Name = nameInput.Text;
            if (double.TryParse(massInput.Text, out double mass))
            {
                celestialBody.Mass = mass;
            }
            if (double.TryParse(xInput.Text, out double x))
            {
                celestialBody.X = x;
            }
            if (double.TryParse(yInput.Text, out double y))
            {
                celestialBody.Y = y;
            }

            if (celestialBody is Satellite satellite)
            {
                if (double.TryParse(additionalInput1.Text, out double velocityX))
                {
                    satellite.VelocityX = velocityX;
                }
                if (double.TryParse(additionalInput2.Text, out double velocityY))
                {
                    satellite.VelocityY = velocityY;
                }
            }
            else if (celestialBody is GroundStation groundStation)
            {
                if (double.TryParse(additionalInput1.Text, out double detectionRange))
                {
                    groundStation.DetectionRange = detectionRange;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}