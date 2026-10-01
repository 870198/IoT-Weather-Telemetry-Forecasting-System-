# IoT-Weather-Telemetry-Forecasting-System-

This is a IoT project i made that uses C# and python ML model and saved data to predict the weather by utilizing a Arduino device.

Attached to the Repo is a User guide that will help you and tell you what to activate to get this to work with screenshots of Success and trouble shooting issues.

To access the Graphs of the data you have to go to the runtime and it will be labeled "Humidity" "Pressure" "Light" graphs which you need to delete to update.

How the full system works – Tips for broader usage

Layer 1 — Arduino (the hardware)
The Arduino sketch runs in a loop and reads all four sensors every 5 seconds. It prints a single JSON line to USB serial:
json
{"sensorId":"MKR1010","temperature":22.50,"humidity":55.30,"pressure":101.32,"light":240}
Nothing else happens here. It just keeps printing that line forever at 9600 baud on whatever COM port Windows assigns it (usually COM4 – if you are using a different port you will have to change this within the serialcode script and prerequisites).
 
Layer 2 — SerialBridge (C# — the relay)
This console app opens COM4, reads each line as it arrives, and forwards it onward. Right now the script you shared only prints the raw line — the HTTP POST to localhost:5000/telemetry still needs to be added (I flagged this earlier). Once complete, for every JSON line it receives it will:
1.	Deserialise the JSON into a TelemetryDto
2.	HTTP POST it to the TelemetryServer
3.	Log success or retry on failure
 
Layer 3 — TelemetryServer (C# — port 5000, the gatekeeper)
A minimal C# API with two routes:
•	GET /health → confirms it's alive
•	POST /telemetry → validates the incoming reading and writes it to SQLite via DataManager using a thread-safe lock so concurrent writes don't corrupt the DB
Each sensor type goes into its own table (TemperatureSensor, HumiditySensor, PressureSensorData, LightSensor) with IsValid and IsAlert flags.
 
Layer 4 — sensors.db (SQLite — the store)
All readings live here at:
Documents\PansIoTassignment\sensors.db (Different for each user)
The IsValid flag is important — only valid rows are read by the Dashboard, and only valid rows are used to generate data.txt for training.
 
Layer 5 — DashboardApp (C# — the brain)
This reads from SQLite, generates graphs, and calls the ML service. Its components:
•	DataManager — queries all valid readings for a given sensorId
•	GraphManager — uses ScottPlot to render temperature.png, humidity.png, pressure.png
•	FeatureBuilder — takes the last N readings and builds the PredictRequest JSON body
•	PredictionClient — POSTs to localhost:8000/predict and handles the response gracefully if the ML service is offline
 
Layer 6 — ml_service.py (Python — port 8000, the model)
This is the script you now have. It has two modes:
Run as a service: python -m uvicorn app:app --port 8000


You will need:
What they need
Python 3.8 or higher
bash
python --version
These packages installed:
bash
python -m pip install fastapi uvicorn scikit-learn numpy pydantic
 
What could go wrong
1. Port 8000 already in use If something else is running on port 8000 you'll get an address already in use error. Fix:
bash
python -m uvicorn app:app --port 8001
But remember to update the C# PredictionClient to match the new port.

2. Wrong directory in the terminal The terminal must be inside the ml_service folder when you run the command. If not:
bash
cd "C:\Users\basment dweller\Desktop\ml_service"
python -m uvicorn app:app --port 8000
3. Virtual environment (.venv) conflict There's already a .venv folder in the directory. If packages were installed inside it but the venv isn't activated, Python won't find them. Either activate it first:
bash
.venv\Scripts\activate
python -m uvicorn app:app --port 8000
Or reinstall packages outside the venv with the python -m pip install command above.

4. Python not in PATH If python isn't recognised at all, Python wasn't added to PATH during install. Fix by reinstalling Python and ticking "Add Python to PATH", or use the full path:
bash
C:\Users\{INSERT USER NAME}\AppData\Local\Programs\Python\Python311\python.exe -m uvicorn app:app --port 8000

6. Firewall blocking port 8000 Windows Firewall may block the C# DashboardApp from reaching the Python service. If the dashboard can't connect, allow port 8000 through Windows Firewall or run both on the same machine (which you already are — localhost should bypass this).

7. data.txt missing This is handled — the service falls back to synthetic training data automatically and will print:
data.txt not found — using synthetic training data.
This is fine until you have real sensor readings to generate it from.

8. C# sends wrong JSON field names The PredictRequest expects exactly these field names:
sensor_id, timestamps, temperatures, humidities, pressures, light_levels
If the C# FeatureBuilder uses different casing or names, the fields will arrive as empty and the model will fall back to heuristic mode silently.

9. Service crashes mid-run If the Arduino stops sending data or the serial bridge disconnects, the ML service itself keeps running fine — it only responds when called. But if app.py crashes for any reason, the DashboardApp is designed to continue without it gracefully.

10. Not enough temperature readings The /predict endpoint needs at least 2 temperature readings to calculate a trend. If the C# FeatureBuilder sends only 1, it will return:
json
{"label": "NotEnoughData", "score": 0.0, ...}
Make sure the dashboard collects at least 2 readings before calling /predict.#
Visual diagrams and how it should look if you did it right:

 



