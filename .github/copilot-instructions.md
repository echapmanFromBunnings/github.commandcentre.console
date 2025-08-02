# GitHub Workflow Manager - Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a Blazor Server .NET application for managing and executing GitHub workflows. The application integrates with GitHub repositories to provide a web-based interface for workflow management.

## Architecture Guidelines
- Use Blazor Server components with proper state management
- Implement secure token storage using ASP.NET Core Data Protection
- Follow the repository pattern for data access
- Use dependency injection for service management
- Implement proper error handling and logging
- Use SignalR for real-time updates

## Key Technologies
- **Blazor Server**: Interactive server-side rendering
- **Octokit**: GitHub API integration
- **YamlDotNet**: YAML parsing for workflow files
- **SignalR**: Real-time communication
- **ASP.NET Core Data Protection**: Secure token storage

## Coding Standards
- Use async/await patterns for all API calls
- Implement proper exception handling with try-catch blocks
- Follow C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use nullable reference types and handle null values appropriately
- Implement proper disposal of resources using `using` statements
- Add comprehensive logging for debugging and monitoring

## Security Considerations
- Never expose GitHub tokens in client-side code
- Validate all user inputs
- Implement proper CORS policies
- Use HTTPS in production
- Validate webhook signatures from GitHub

## Component Structure
- Place reusable components in `Components/Shared`
- Keep page components in `Components/Pages`
- Use component parameters for data passing
- Implement proper component lifecycle methods

## Services Pattern
- Create service interfaces in `Services/Interfaces`
- Implement services in `Services/Implementations`
- Register services in `Program.cs`
- Use scoped lifetime for stateful services
