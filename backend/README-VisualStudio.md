# Running Both Backend Services in Visual Studio

This solution is configured to run both the **ApiGateway** and **IncidentService** projects simultaneously for easier debugging.

## Setup Instructions

### 1. Open the Solution
- Open `backend/IncidentScope.sln` in Visual Studio

### 2. Configure Multiple Startup Projects

1. **Right-click** on the solution in Solution Explorer
2. Select **Properties**
3. In the left panel, select **Startup Project**
4. Select **Multiple startup projects**
5. Set both projects to **Start**:
   - `IncidentScope.ApiGateway` → **Start**
   - `IncidentScope.IncidentService` → **Start**
6. Click **OK**

### 3. Run the Solution

- Press **F5** or click the **Start** button
- Both services will start simultaneously:
  - **ApiGateway**: http://localhost:5000/swagger
  - **IncidentService**: http://localhost:5001/swagger

## Debugging

- Set breakpoints in either project
- Both projects will pause at breakpoints when debugging
- You can step through code in both services simultaneously

## Port Configuration

- **ApiGateway**: Port 5000 (configured in `Properties/launchSettings.json`)
- **IncidentService**: Port 5001 (configured in `Properties/launchSettings.json`)

## Troubleshooting

If you get port conflicts:
- Make sure no other instances are running on ports 5000 or 5001
- Check Task Manager for `IncidentScope.ApiGateway.exe` or `IncidentScope.IncidentService.exe` processes
- Kill any existing processes: `taskkill /F /IM IncidentScope.ApiGateway.exe`

