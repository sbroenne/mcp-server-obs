import * as vscode from 'vscode';
import * as path from 'path';

let statusBarItem: vscode.StatusBarItem;

export async function activate(context: vscode.ExtensionContext) {
    console.log('OBS MCP Server extension is now active');

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

    // Create status bar item
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.text = '$(record) OBS MCP';
    statusBarItem.tooltip = 'OBS MCP Server';
    statusBarItem.command = 'obs-mcp.showStatus';
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

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
                    'ObsMcp - OBS Studio Automation',
                    serverPath,
                    [],
                    env,
                    '0.0.3'
                )
            ];
        }
    });
    context.subscriptions.push(mcpProvider);

    // Register commands
    const connectCommand = vscode.commands.registerCommand('obs-mcp.connect', async () => {
        vscode.window.showInformationMessage('ObsMcp: Use the MCP tools in Copilot Chat to connect to OBS Studio. Try: "Connect to OBS"');
    });
    context.subscriptions.push(connectCommand);

    const disconnectCommand = vscode.commands.registerCommand('obs-mcp.disconnect', async () => {
        vscode.window.showInformationMessage('ObsMcp: Use the MCP tools in Copilot Chat to disconnect. Try: "Disconnect from OBS"');
    });
    context.subscriptions.push(disconnectCommand);

    const showStatusCommand = vscode.commands.registerCommand('obs-mcp.showStatus', async () => {
        const config = vscode.workspace.getConfiguration('obs-mcp');
        const host = config.get<string>('host', 'localhost');
        const port = config.get<number>('port', 4455);
        
        const message = `OBS MCP Server Configuration:\nHost: ${host}:${port}\n\nUse Copilot Chat with MCP tools to control OBS Studio.`;
        
        const action = await vscode.window.showInformationMessage(
            message,
            'Open Settings',
            'Open Copilot Chat'
        );

        if (action === 'Open Settings') {
            vscode.commands.executeCommand('workbench.action.openSettings', 'obs-mcp');
        } else if (action === 'Open Copilot Chat') {
            vscode.commands.executeCommand('workbench.action.chat.open');
        }
    });
    context.subscriptions.push(showStatusCommand);

    // Show activation message
    if (vscode.workspace.getConfiguration('obs-mcp').get<boolean>('autoConnect', false)) {
        vscode.window.showInformationMessage('ObsMcp ready. OBS tools are available in Copilot Chat.');
    }
}

export function deactivate() {
    if (statusBarItem) {
        statusBarItem.dispose();
    }
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

