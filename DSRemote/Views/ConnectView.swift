import SwiftUI

struct ConnectView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @EnvironmentObject private var layoutService: LayoutService
    @State private var host = ""
    @State private var port = "9876"
    @State private var showSettings = false
    @State private var showChangeStuff = false
    let onConnected: () -> Void

    private let repoURL = URL(string: "https://github.com/anomalyco/DSRemote/releases")!

    var body: some View {
        ZStack {
            Color(hex: "#1a1a2e").ignoresSafeArea()

            VStack(spacing: 20) {
                // Top bar with update button
                HStack {
                    Button(action: {
                        UIApplication.shared.open(repoURL)
                    }) {
                        HStack(spacing: 4) {
                            Image(systemName: "arrow.down.circle")
                                .font(.caption)
                            Text("Update via GitHub")
                                .font(.caption)
                        }
                        .foregroundColor(settings.accentColor)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 6)
                        .background(settings.accentColor.opacity(0.15))
                        .cornerRadius(8)
                    }

                    Spacer()
                }
                .padding(.horizontal)
                .padding(.top, 50)

                Spacer()

                ZStack {
                    Circle()
                        .fill(settings.accentColor.opacity(0.15))
                        .frame(width: 100, height: 100)
                    Image(systemName: "antenna.radiowaves.left.and.right")
                        .font(.system(size: 44))
                        .foregroundColor(settings.accentColor)
                }

                Text("DSRemote")
                    .font(.system(size: 34, weight: .bold))
                    .foregroundColor(.white)

                Text(network.connectionStatus)
                    .font(.subheadline)
                    .foregroundColor(network.isConnected ? .green : .gray)

                if !network.discoveredHosts.isEmpty {
                    VStack(spacing: 8) {
                        Text("Discovered PCs:")
                            .font(.caption)
                            .foregroundColor(.gray)
                        ForEach(network.discoveredHosts, id: \.self) { ip in
                            Button(action: {
                                host = ip
                                connect()
                            }) {
                                HStack {
                                    Image(systemName: "desktopcomputer")
                                        .foregroundColor(settings.accentColor)
                                    Text(ip)
                                        .foregroundColor(.white)
                                    Spacer()
                                    Text("Tap to connect")
                                        .font(.caption)
                                        .foregroundColor(.gray)
                                }
                                .padding()
                                .background(Color(hex: "#16213e"))
                                .cornerRadius(10)
                            }
                        }
                    }
                    .padding(.horizontal, 30)
                }

                VStack(spacing: 12) {
                    HStack {
                        Image(systemName: "network")
                            .foregroundColor(settings.accentColor)
                        TextField("PC IP Address", text: $host)
                            .textContentType(.URL)
                            .keyboardType(.decimalPad)
                            .foregroundColor(.white)
                            .autocorrectionDisabled()
                    }
                    .padding()
                    .background(Color(hex: "#16213e"))
                    .cornerRadius(10)

                    HStack {
                        Image(systemName: "number")
                            .foregroundColor(settings.accentColor)
                        TextField("Port", text: $port)
                            .keyboardType(.numberPad)
                            .foregroundColor(.white)
                    }
                    .padding()
                    .background(Color(hex: "#16213e"))
                    .cornerRadius(10)
                }
                .padding(.horizontal, 30)

                Button(action: connect) {
                    Text(network.isConnected ? "Connected!" : "Connect")
                        .fontWeight(.semibold)
                        .frame(maxWidth: .infinity)
                        .padding()
                        .background(network.isConnected ? Color.green : settings.accentColor)
                        .foregroundColor(.white)
                        .cornerRadius(12)
                }
                .disabled(network.isConnected)
                .padding(.horizontal, 30)

                Button(action: { showSettings.toggle() }) {
                    Text("Customize Color")
                        .foregroundColor(.gray)
                        .underline()
                        .font(.caption)
                }

                Spacer()

                HStack {
                    Text("Make sure PC and iPhone are on the same network")
                        .font(.caption)
                        .foregroundColor(.gray)

                    Spacer()

                    Button(action: { showChangeStuff = true }) {
                        HStack(spacing: 4) {
                            Image(systemName: "slider.horizontal.3")
                                .font(.caption)
                            Text("Change Stuff")
                                .font(.caption)
                        }
                        .foregroundColor(settings.accentColor)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 6)
                        .background(settings.accentColor.opacity(0.15))
                        .cornerRadius(8)
                    }
                }
                .padding(.horizontal)
                .padding(.bottom, 30)
            }
        }
        .onAppear {
            host = settings.lastHost
            port = String(settings.lastPort)
            network.startDiscovery()
        }
        .sheet(isPresented: $showSettings) {
            SettingsView()
        }
        .sheet(isPresented: $showChangeStuff) {
            ChangeStuffView()
                .environmentObject(settings)
                .environmentObject(layoutService)
        }
    }

    private func connect() {
        settings.lastHost = host
        settings.lastPort = Int(port) ?? 9876
        network.configure(
            host: host,
            port: Int(port) ?? 9876,
            onScreenshot: { _ in },
            onDisconnect: {}
        )
        network.connect()

        DispatchQueue.main.asyncAfter(deadline: .now() + 1.5) {
            if network.isConnected {
                onConnected()
            }
        }
    }
}
