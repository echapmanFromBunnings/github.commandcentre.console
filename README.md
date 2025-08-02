# GitHub Workflow Manager

A modern Blazor Server application for managing and executing GitHub Actions workflows with an intuitive web interface, intelligent caching, and comprehensive workflow metadata parsing.

## 🚀 Features

### Core Functionality
- **Workflow Discovery**: Automatically discovers and displays all GitHub Actions workflows from your repository
- **Interactive Dashboard**: Clean, responsive interface showing workflow status, recent runs, and metadata
- **Manual Workflow Execution**: Trigger workflows with custom input parameters directly from the web interface
- **Real-time Updates**: Live status updates using SignalR for workflow execution feedback
- **Performance Optimized**: Intelligent caching system for fast dashboard loading (5-minute workflow cache, 10-minute config cache)

### Advanced Features
- **Workflow Metadata Parsing**: Extracts structured documentation from workflow comments including:
  - Purpose and description
  - Trigger conditions
  - Scope and actions
  - Input parameter descriptions
- **Recent Run Tracking**: Displays latest execution status, run numbers, and links to GitHub
- **Secure Token Management**: Encrypted storage of GitHub Personal Access Tokens using ASP.NET Core Data Protection
- **RESTful API**: Backend API endpoints for programmatic access to workflow data
- **Auto-refresh**: Workflow run statuses update automatically every 30 seconds
- **Parallel Processing**: Concurrent workflow run fetching for improved performance

## Features

### 🔧 Core Functionality
- **GitHub Integration**: Secure connection to GitHub repositories using Personal Access Tokens
- **Workflow Discovery**: Automatic scanning and parsing of workflow files from any repository
- **Real-time Monitoring**: Live updates on workflow execution status and progress
- **Manual Execution**: Trigger workflows with custom parameters through a user-friendly interface
- **Webhook Support**: Receive real-time notifications from GitHub for workflow events

### 🎯 Key Capabilities
- **Dynamic Form Generation**: Automatically create input forms based on workflow parameters
- **Parameter Validation**: Ensure required inputs are provided with proper type checking
- **Execution History**: View detailed run history with status, duration, and links to GitHub
- **Secure Storage**: Encrypted storage of GitHub tokens and configuration
- **Responsive Design**: Works seamlessly on desktop and mobile devices

### 🚀 Advanced Features
- **SignalR Integration**: Real-time updates without page refresh
- **Workflow Parsing**: YAML parsing to extract triggers, inputs, and metadata
- **Error Handling**: Comprehensive error handling with user-friendly messages
- **Rate Limiting**: Proper handling of GitHub API rate limits
- **Multi-Repository**: Configurable for any GitHub repository

## 🏗️ Architecture

### Technology Stack
- **Frontend**: Blazor Server with Bootstrap 5 for responsive UI
- **Backend**: ASP.NET Core 8.0 with dependency injection
- **Real-time**: SignalR for live updates
- **API Integration**: Octokit.NET for GitHub API communication
- **Caching**: ASP.NET Core Memory Caching for performance optimization
- **Security**: Data Protection APIs for secure token storage
- **YAML Processing**: YamlDotNet for workflow file parsing

### Project Structure
```
GitHubWorkflowManager/
├── Components/
│   ├── Layout/           # Application layout components
│   ├── Pages/            # Blazor page components
│   └── Shared/           # Reusable UI components
├── Controllers/          # Web API controllers
├── Services/
│   ├── Interfaces/       # Service contracts
│   └── Implementations/  # Service implementations
├── Models/               # Data models and DTOs
├── Hubs/                 # SignalR hubs
└── App_Data/             # Application data and configuration
```

### Performance Features
- **Intelligent Caching**: Workflows cached for 5 minutes, configuration for 10 minutes
- **API Separation**: Static workflow data cached separately from dynamic run data
- **Parallel Loading**: Workflow runs fetched concurrently for better performance
- **Auto-refresh**: Background updates every 30 seconds without blocking UI

## Prerequisites

- .NET 8.0 SDK or later
- A GitHub Personal Access Token with appropriate permissions
- Access to a GitHub repository containing workflow files

## Quick Start

### 1. Clone and Setup
```bash
git clone <repository-url>
cd github.workflow.manager
dotnet restore
```

### 2. Run the Application
```bash
dotnet run
```

### 3. Initial Configuration
1. Navigate to `https://localhost:5001` (or the displayed URL)
2. Click "Configure GitHub Connection" on the setup page
3. Enter your GitHub Personal Access Token
4. Specify the repository owner and name
5. Configure the workflows directory path (default: `.github/workflows`)
6. Optionally set a webhook secret for enhanced security

### 4. GitHub Personal Access Token Setup
1. Go to [GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens)
2. Click "Generate new token (classic)"
3. Set an appropriate expiration date
4. Select the following scopes:
   - `repo` (Full control of private repositories)
   - `workflow` (Update GitHub Action workflows)
5. Copy the generated token and use it in the application

## Configuration

### Environment Variables
The application supports configuration through environment variables:

```bash
# Optional: Custom data protection key storage
DataProtection__KeyPath=/path/to/keys

# Optional: Custom application name for data protection
DataProtection__ApplicationName=GitHubWorkflowManager
```

### Application Settings
Configuration is stored securely using ASP.NET Core Data Protection in the `App_Data` directory.

## Webhook Configuration

To receive real-time updates from GitHub:

1. Complete the initial setup in the application
2. Note the webhook URL provided: `https://your-domain/api/webhook/github`
3. In your GitHub repository, go to Settings → Webhooks
4. Click "Add webhook"
5. Configure:
   - **Payload URL**: Your webhook URL
   - **Content type**: `application/json`
   - **Secret**: Use the webhook secret from your configuration (optional but recommended)
   - **Events**: Select "Workflow runs" and "Workflow jobs"
6. Click "Add webhook"

## Usage Guide

### Dashboard
- View all workflows in your repository
- See current status and last run information
- Quick access to manual execution for workflow_dispatch workflows
- Summary statistics of your workflow activity

### Workflow Metadata Parsing
The application automatically parses structured comments from workflow files to provide rich documentation. Use this format in your workflow files:

```yaml
# =============================================================================
# WORKFLOW: Your Workflow Name
# =============================================================================
# PURPOSE: Description of what this workflow does
# TRIGGER: When this workflow runs (manual, push, PR, schedule, etc.)
# SCOPE: What repositories or conditions this applies to
# ACTIONS:
#   - Step 1: Description of first action
#   - Step 2: Description of second action
#   - Step 3: Description of third action
# INPUTS:
#   - input1: Description of first input parameter
#   - input2: Description of second input parameter
# =============================================================================
```

This metadata is automatically extracted and displayed in the dashboard, providing:
- **Clear Purpose**: What the workflow accomplishes
- **Trigger Information**: When and how the workflow executes
- **Scope Details**: What it affects or operates on
- **Action Steps**: Breakdown of what the workflow does
- **Input Documentation**: Description of required parameters

### Dashboard Features

#### Workflow Cards
Each workflow is displayed as a card showing:
- **Workflow Name**: Parsed from the workflow file
- **Status Badge**: Current status (success, failure, in progress, etc.)
- **Description**: Extracted from workflow metadata or file comments
- **Triggers**: Display of trigger conditions (push, pull_request, workflow_dispatch, etc.)
- **Last Run**: Information about the most recent execution with run number and timestamp
- **Parameters**: Number of input parameters for manual workflows
- **Metadata**: Purpose, scope, and actions when available

### Workflow Parameters
The application supports all GitHub Actions input types:
- **String**: Text input fields
- **Boolean**: Checkbox controls
- **Choice**: Dropdown selection from predefined options
- **Environment**: Text input with environment context

## Security Features

### Token Security
- GitHub Personal Access Tokens are encrypted using ASP.NET Core Data Protection
- Tokens are never exposed in client-side code or logs
- Secure key storage with automatic key rotation

### Webhook Security
- Optional webhook signature verification
- Request validation and payload sanitization
- CORS protection and secure headers

### Input Validation
- Comprehensive validation of all user inputs
- Protection against injection attacks
- Proper encoding of displayed content

## 🔌 API Reference

### Workflow API Endpoints

#### GET `/api/WorkflowApi/workflows`
Returns cached list of all workflows with metadata.

**Response**: Array of workflow objects including:
- Basic workflow information (name, description, triggers)
- Parsed metadata (purpose, scope, actions, inputs)
- File information and paths

#### GET `/api/WorkflowApi/workflow-runs/latest`
Returns latest run information for specified workflows.

**Query Parameters**:
- `workflowNames[]`: Array of workflow names to get run info for

**Response**: Dictionary mapping workflow names to latest run information including:
- Run ID, number, and status
- Creation and update timestamps
- Conclusion (success, failure, etc.)
- GitHub HTML URL for run details

#### POST `/api/WorkflowApi/workflow/trigger`
Triggers a workflow execution with optional input parameters.

**Request Body**:
```json
{
  "workflowId": "workflow-name",
  "inputs": {
    "param1": "value1",
    "param2": "value2"
  }
}
```

**Response**: Execution result with success status and any error messages

#### DELETE `/api/WorkflowApi/cache/workflows`
Clears the workflow cache to force refresh from GitHub.

**Response**: Success confirmation

## Troubleshooting

### Common Issues

## 🐛 Troubleshooting

### Common Issues

#### "Invalid request URI" Error
**Cause**: HttpClient base address configuration issue
**Solution**: This is automatically handled in the current version. If you encounter this, restart the application.

#### Workflows Not Loading
**Cause**: GitHub token permissions, repository access, or network issues
**Solution**: 
1. Verify your GitHub token has correct permissions (`repo`, `actions`, `metadata`)
2. Ensure the repository owner/name is correct in configuration
3. Check the browser console and application logs for detailed error messages
4. Try the "Refresh" button to clear cache and reload

#### Recent Runs Not Displaying
**Cause**: GitHub API rate limiting or workflow execution history
**Solution**: 
1. Check GitHub API rate limits in your account
2. Verify workflows have been executed recently
3. Use the refresh button to force reload
4. Check application logs for GitHub API errors

#### Configuration Lost on Restart
**Cause**: Data Protection key storage issues
**Solution**: 
1. Ensure `App_Data/DataProtection-Keys` directory exists and is writable
2. Check application has proper file system permissions
3. Verify data protection configuration in startup

#### Slow Dashboard Loading
**Cause**: Cache not populated or large number of workflows
**Solution**:
1. First load will be slower while populating cache
2. Subsequent loads should be much faster (cache lasts 5 minutes)
3. Use browser developer tools to check API response times

#### Workflow Metadata Not Showing
**Cause**: Metadata format not recognized or parsing errors
**Solution**:
1. Ensure metadata follows the exact format specified in documentation
2. Check for proper comment syntax in YAML files
3. Verify workflow files are valid YAML
4. Check application logs for parsing errors

### Performance Optimization

#### Cache Behavior
- **Workflow Cache**: 5 minutes - cleared manually with refresh button
- **Configuration Cache**: 10 minutes - automatically managed
- **Auto-refresh**: 30 seconds for workflow run status updates

#### Monitoring Performance
- Check browser Network tab for API call timing
- Monitor application logs for cache hit/miss information
- Use browser console to see client-side performance metrics

### Debugging
Enable detailed logging by setting the log level in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "GitHubWorkflowManager": "Debug"
    }
  }
}
```

## Development

### Project Structure
```
GitHubWorkflowManager/
├── Components/
│   ├── Layout/          # Application layout components
│   ├── Pages/           # Page components (Dashboard, Setup, etc.)
│   └── Shared/          # Reusable UI components
├── Controllers/         # API controllers (Webhook)
├── Hubs/               # SignalR hubs
├── Models/             # Data models and DTOs
├── Services/           # Business logic services
│   ├── Interfaces/     # Service contracts
│   └── Implementations/ # Service implementations
└── wwwroot/            # Static files
```

### Key Services
- **IGitHubConfigurationService**: Manages secure configuration storage
- **IGitHubService**: Handles GitHub API interactions
- **IWorkflowParsingService**: Parses YAML workflow files
- **IWebhookService**: Processes GitHub webhook events
- **INotificationService**: Manages real-time notifications

### Adding Features
1. Define service interfaces in `Services/Interfaces/`
2. Implement services in `Services/Implementations/`
3. Register services in `Program.cs`
4. Create UI components in `Components/`
5. Add any necessary models in `Models/`

## Deployment

### Local Development
```bash
dotnet run --environment Development
```

### Production Deployment
1. Configure production settings in `appsettings.Production.json`
2. Set up HTTPS certificates
3. Configure reverse proxy (if needed)
4. Set up data protection key storage
5. Deploy using your preferred method (IIS, Docker, etc.)

### Docker Support
Create a `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GitHubWorkflowManager.csproj", "."]
RUN dotnet restore "GitHubWorkflowManager.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "GitHubWorkflowManager.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GitHubWorkflowManager.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GitHubWorkflowManager.dll"]
```

## 🤝 Contributing

### Development Guidelines
- Follow C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use async/await patterns for all API calls
- Implement proper exception handling with try-catch blocks
- Add comprehensive logging for debugging and monitoring
- Use nullable reference types and handle null values appropriately
- Implement proper disposal of resources using `using` statements

### Adding New Features

#### Service Development
1. Define service interfaces in `Services/Interfaces/`
2. Implement services in `Services/Implementations/`
3. Register services in `Program.cs` with appropriate lifetime (Scoped, Singleton, Transient)
4. Add comprehensive logging and error handling

#### UI Development
1. Create reusable components in `Components/Shared/`
2. Place page components in `Components/Pages/`
3. Use component parameters for data passing
4. Implement proper component lifecycle methods (`OnInitializedAsync`, `OnParametersSetAsync`)
5. Follow Bootstrap 5 conventions for consistent styling

#### API Development
1. Add controllers in `Controllers/` directory
2. Use proper HTTP status codes and response types
3. Implement input validation and error handling
4. Add comprehensive logging for API calls
5. Follow RESTful conventions

### Testing
- Unit tests for service classes
- Integration tests for API endpoints
- Component testing for UI elements
- Performance testing for caching behavior

### Pull Request Process
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the guidelines above
4. Add tests if applicable
5. Update documentation as needed
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request with detailed description

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

### Dependencies
- **[Octokit.NET](https://github.com/octokit/octokit.net)**: Excellent GitHub API integration library
- **[YamlDotNet](https://github.com/aaubry/YamlDotNet)**: Robust YAML parsing for workflow files
- **[ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)**: Powerful web framework with Blazor Server
- **[Bootstrap](https://getbootstrap.com/)**: Responsive UI framework with modern components
- **[SignalR](https://docs.microsoft.com/en-us/aspnet/core/signalr/)**: Real-time web functionality

### Special Thanks
- The ASP.NET Core team for the excellent Blazor Server framework
- The GitHub team for comprehensive API documentation
- The open-source community for the fantastic libraries used in this project

## 📞 Support

### Getting Help
1. **Documentation**: Review this README and inline code comments
2. **GitHub Issues**: Search existing issues or create a new one
3. **Troubleshooting**: Check the troubleshooting section above
4. **Logs**: Enable debug logging for detailed information

### Reporting Issues
When reporting issues, please include:
- Steps to reproduce the problem
- Expected vs actual behavior
- Browser and operating system information
- Relevant log messages or error details
- Screenshots if applicable

### Feature Requests
We welcome feature requests! Please:
- Check if the feature already exists or is planned
- Describe the use case and benefits
- Provide mockups or examples if helpful
- Consider contributing the implementation

---

**Built with ❤️ using ASP.NET Core and Blazor Server**

*Empowering teams to manage GitHub Actions workflows with confidence and efficiency.*
