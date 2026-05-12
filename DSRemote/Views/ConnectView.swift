import SwiftUI

struct ConnectView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @State private var host = ""
    @State private var port = "9876"
    @State private var showSettings = false
    let onConnected: () -> Void

    var body: some View {
        VStack(spacing: 25) {
            Spacer()

            ZStack {
                Circle()
                    .fill(settings.accentColor.opacity(0.15))
                    .frame(width: 120, height: 120)
                Image(systemName: "antenna.radiowaves.left.and.right")
                    .font(.system(size: 50))
                    .foregroundColor(settings.accentColor)
            }

            Text("DSRemote")
                .font(.system(size: 36, weight: .bold))
                .foregroundColor(.white)

            Text(network.connectionStatus)
                .font(.subheadline)
                .foregroundColor(network.isConnected ? .green : .gray)

            VStack(spacing: 12) {
                HStack {
                    Image(systemName: "network")
                        .foregroundColor(settings.accentColor)
                    TextField("PC IP Address", text: $host)
                        .textContentType(.URL)
                        .keyboardType(.decimalPad)
                        .foregroundColor(.white)
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
            }

            Spacer()

            Text("Connect via WiFi\nMake sure PC and iPhone are on the same network")
                .font(.caption)
                .foregroundColor(.gray)
                .multilineTextAlignment(.center)
                .padding(.bottom, 30)
        }
        .onAppear {
            host = settings.lastHost
            port = String(settings.lastPort)
        }
        .sheet(isPresented: $showSettings) {
            SettingsView()
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
