import SwiftUI

enum AppScreen {
    case connect
    case gameList
    case emulator
}

struct ContentView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @State private var screen: AppScreen = .connect
    @State private var topScreenImage: UIImage?
    @State private var bottomScreenImage: UIImage?

    var body: some View {
        ZStack {
            Color(hex: "#1a1a2e").ignoresSafeArea()

            switch screen {
            case .connect:
                ConnectView(onConnected: {
                    screen = .gameList
                })
            case .gameList:
                GameListView(onLaunchGame: { game in
                    network.sendLaunchGame(game)
                    screen = .emulator
                }, onDisconnect: {
                    network.disconnect()
                    screen = .connect
                })
            case .emulator:
                EmulatorView(
                    topScreen: $topScreenImage,
                    bottomScreen: $bottomScreenImage,
                    onPowerOff: {
                        screen = .gameList
                    },
                    onDisconnect: {
                        network.disconnect()
                        screen = .connect
                    }
                )
            }
        }
        .onAppear {
            network.configure(
                host: settings.lastHost.isEmpty ? "192.168.1.100" : settings.lastHost,
                port: settings.lastPort,
                onScreenshot: { data in
                    DispatchQueue.main.async {
                        processScreenshot(data)
                    }
                },
                onDisconnect: {
                    DispatchQueue.main.async {
                        screen = .connect
                    }
                }
            )
        }
    }

    private func processScreenshot(_ data: Data) {
        if let image = UIImage(data: data) {
            if topScreenImage == nil {
                topScreenImage = image
            } else {
                bottomScreenImage = image
            }
        }
    }
}
