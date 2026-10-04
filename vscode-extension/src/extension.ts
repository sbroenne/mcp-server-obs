import * as vscode from 'vscode';
import * as path from 'path';

export async function activate(context: vscode.ExtensionContext) {
    console.log('OBS Studio MCP Server extension is now active');

    let dotnetPath: string;
    try {
        dotnetPath = await ensureDotNetRuntime(context.extension.id);
    } catch (error) {
        const errorMessage = error instanceof Error ? error.message : String(error);
        vscode.window.showErrorMessage(
            `OBS MCP: Failed to setup .NET environment: ${errorMessage}. ` +
            `The MCP server was not registered.`
        );
        throw error;
    }

    const definitionsChanged = new vscode.EventEmitter<void>();
    context.subscriptions.push(definitionsChanged);
    context.subscriptions.push(vscode.workspace.onDidChangeConfiguration(event => {
        if (event.affectsConfiguration('obs-mcp')) {
            definitionsChanged.fire();
        }
    }));

    const mcpProvider = vscode.lm.registerMcpServerDefinitionProvider('obs-mcp', {
        onDidChangeMcpServerDefinitions: definitionsChanged.event,
        provideMcpServerDefinitions: () => {
            const config = vscode.workspace.getConfiguration('obs-mcp');
            const host = config.get<string>('host', 'localhost');
            const port = config.get<number>('port', 4455);
            const password = config.get<string>('password', '');

            const serverPath = path.join(context.extensionPath, 'bin', 'Sbroenne.ObsMcp.McpServer.dll');

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
                    dotnetPath,
                    [serverPath],
                    env,
                    context.extension.packageJSON.version
                )
            ];
        }
    });
    context.subscriptions.push(mcpProvider);
}

export function deactivate() {
    // Nothing to clean up
}

async function ensureDotNetRuntime(requestingExtensionId: string): Promise<string> {
    try {
        // Request .NET runtime acquisition via the .NET Install Tool extension
        const dotnetExtension = vscode.extensions.getExtension('ms-dotnettools.vscode-dotnet-runtime');

        if (!dotnetExtension) {
            throw new Error('.NET Install Tool extension not found. Please install ms-dotnettools.vscode-dotnet-runtime');
        }

        if (!dotnetExtension.isActive) {
            await dotnetExtension.activate();
        }

        const result = await vscode.commands.executeCommand<{ dotnetPath: string }>('dotnet.acquire', {
            version: '10.0',
            mode: 'runtime',
            requestingExtensionId
        });

        if (!result?.dotnetPath) {
            throw new Error('.NET Install Tool did not return a .NET 10 runtime path');
        }

        console.log(`OBS MCP: .NET runtime available at ${result.dotnetPath}`);
        return result.dotnetPath;
    } catch (error) {
        console.error('OBS MCP: Error during .NET runtime setup:', error);
        throw error;
    }
}
