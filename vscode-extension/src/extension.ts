import * as vscode from 'vscode';
import * as path from 'path';

export async function activate(context: vscode.ExtensionContext) {
    console.log('OBS Studio MCP Server extension is now active');

    // Ensure .NET runtime is available
    try {
        await ensureDotNetRuntime();
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        vscode.window.showErrorMessage(
            `OBS MCP: Failed to setup .NET environment: ${errorMessage}. ` +
            `The extension may not work correctly.`
        );
    }

    // Register MCP Server Definition Provider
    const mcpProvider = vscode.lm.registerMcpServerDefinitionProvider('obs-mcp', {
        provideMcpServerDefinitions: () => {
            const config = vscode.workspace.getConfiguration('obs-mcp');
            const host = config.get<string>('host', 'localhost');
            const port = config.get<number>('port', 4455);
            const password = config.get<string>('password', '');

            // Path to the .NET MCP server executable
            const serverPath = path.join(context.extensionPath, 'bin', 'Sbroenne.ObsMcp.McpServer.exe');

            // Create environment variables for OBS connection
            const env: Record<string, string> = {
                'OBS_HOST': host,
                'OBS_PORT': port.toString()
            };
            if (password) {
                env['OBS_PASSWORD'] = password;
            }

            return [
                new vscode.McpStdioServerDefinition(
                    'OBS Studio MCP Server',
                    serverPath,
                    [],
                    env,
                    '0.0.3'
                )
            ];
        }
    });
    context.subscriptions.push(mcpProvider);
}

export function deactivate() {
    // Nothing to clean up
}

async function ensureDotNetRuntime(): Promise<void> {
    try {
        // Request .NET runtime acquisition via the .NET Install Tool extension
        const dotnetExtension = vscode.extensions.getExtension('ms-dotnettools.vscode-dotnet-runtime');

        if (!dotnetExtension) {
            throw new Error('.NET Install Tool extension not found. Please install ms-dotnettools.vscode-dotnet-runtime');
        }

        if (!dotnetExtension.isActive) {
            await dotnetExtension.activate();
        }

        // Request .NET 8 runtime using the command-based API
        const requestingExtensionId = 'sbroenne.obs-mcp';

        await vscode.commands.executeCommand('dotnet.showAcquisitionLog');
        const result = await vscode.commands.executeCommand<{ dotnetPath: string }>('dotnet.acquire', {
            version: '8.0',
            requestingExtensionId
        });

        if (result?.dotnetPath) {
            console.log(`OBS MCP: .NET runtime available at ${result.dotnetPath}`);
        }

        console.log('OBS MCP: .NET runtime setup completed');
    } catch (error) {
        console.error('OBS MCP: Error during .NET runtime setup:', error);
        throw error;
    }
}

