const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');
const vm = require('node:vm');

function loadExtension(options = {}) {
    const calls = [];
    const errors = [];
    let provider;
    let configurationChanged;
    let changeNotifications = 0;
    const runtimeExtension = {
        isActive: false,
        async activate() { calls.push('activate-runtime'); }
    };
    const settings = { host: 'localhost', port: 4455, password: '', ...options.settings };
    const vscode = {
        extensions: {
            getExtension: () => options.missingRuntimeExtension ? undefined : runtimeExtension
        },
        commands: {
            async executeCommand(command, args) {
                calls.push({ command, args });
                if (options.acquisitionError) {
                    throw new Error(options.acquisitionError);
                }
                return options.emptyRuntimePath ? undefined : { dotnetPath: 'C:\\dotnet\\dotnet.exe' };
            }
        },
        window: { showErrorMessage: message => errors.push(message) },
        workspace: {
            getConfiguration: () => ({ get: key => settings[key] }),
            onDidChangeConfiguration: callback => {
                configurationChanged = callback;
                return { dispose() {} };
            }
        },
        EventEmitter: class {
            event = () => {};
            fire() { changeNotifications++; }
            dispose() {}
        },
        lm: {
            registerMcpServerDefinitionProvider(id, value) {
                assert.equal(id, 'obs-mcp');
                provider = value;
                return { dispose() {} };
            }
        },
        McpStdioServerDefinition: class {
            constructor(label, command, args, env, version) {
                Object.assign(this, { label, command, args, env, version });
            }
        }
    };
    const module = { exports: {} };
    const filename = path.join(__dirname, '..', 'out', 'extension.js');
    vm.runInNewContext(fs.readFileSync(filename, 'utf8'), {
        exports: module.exports,
        module,
        require: name => name === 'vscode' ? vscode : require(name),
        console: { log() {}, error() {} }
    }, { filename });
    const context = {
        extensionPath: 'C:\\extensions\\obs-mcp',
        extension: { id: 'sbroenne.obs-mcp', packageJSON: { version: '1.2.3' } },
        subscriptions: []
    };
    return {
        activate: () => module.exports.activate(context),
        calls,
        errors,
        settings,
        get provider() { return provider; },
        get changeNotifications() { return changeNotifications; },
        changeConfiguration: affectsConfiguration => configurationChanged({ affectsConfiguration })
    };
}

test('acquires .NET 10 and starts the bundled DLL with the acquired runtime', async () => {
    const extension = loadExtension();
    await extension.activate();

    assert.equal(extension.calls[0], 'activate-runtime');
    const acquisition = extension.calls[1];
    assert.equal(acquisition.command, 'dotnet.acquire');
    assert.equal(acquisition.args.version, '10.0');
    assert.equal(acquisition.args.mode, 'runtime');
    assert.equal(acquisition.args.requestingExtensionId, 'sbroenne.obs-mcp');

    const definition = extension.provider.provideMcpServerDefinitions()[0];
    assert.equal(definition.command, 'C:\\dotnet\\dotnet.exe');
    assert.equal(definition.args.length, 1);
    assert.equal(definition.args[0], path.join('C:\\extensions\\obs-mcp', 'bin', 'Sbroenne.ObsMcp.McpServer.dll'));
    assert.equal(definition.env.OBS_HOST, 'localhost');
    assert.equal(definition.env.OBS_PORT, '4455');
    assert.equal(definition.env.OBS_PASSWORD, undefined);
    assert.equal(definition.version, '1.2.3');
});

test('refreshes server definitions when OBS settings change', async () => {
    const extension = loadExtension({ settings: { password: 'test-password' } });
    await extension.activate();

    assert.equal(extension.provider.provideMcpServerDefinitions()[0].env.OBS_PASSWORD, 'test-password');
    extension.settings.port = 4456;
    extension.changeConfiguration(section => section === 'obs-mcp');
    assert.equal(extension.changeNotifications, 1);
    assert.equal(extension.provider.provideMcpServerDefinitions()[0].env.OBS_PORT, '4456');

    extension.changeConfiguration(() => false);
    assert.equal(extension.changeNotifications, 1);
});

for (const [name, options, error] of [
    ['missing runtime extension', { missingRuntimeExtension: true }, /Install Tool extension not found/],
    ['failed runtime acquisition', { acquisitionError: 'Download failed' }, /Download failed/],
    ['missing runtime path', { emptyRuntimePath: true }, /did not return a .NET 10 runtime path/]
]) {
    test(`does not register a broken server after ${name}`, async () => {
        const extension = loadExtension(options);
        await assert.rejects(extension.activate(), error);
        assert.equal(extension.provider, undefined);
        assert.equal(extension.errors.length, 1);
        assert.match(extension.errors[0], /MCP server was not registered/);
    });
}
