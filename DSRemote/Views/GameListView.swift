import SwiftUI

struct GameListView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @State private var searchText = ""
    @State private var showLayoutPicker = false
    let onLaunchGame: (GameRom) -> Void
    let onDisconnect: () -> Void

    var filteredGames: [GameRom] {
        if searchText.isEmpty { return network.games }
        return network.games.filter { $0.name.localizedCaseInsensitiveContains(searchText) }
    }

    var body: some View {
        VStack(spacing: 0) {
            header

            if network.games.isEmpty {
                emptyState
            } else if settings.gameSelectLayout == .threeDS {
                ThreeDSGameListView(games: filteredGames, onLaunchGame: onLaunchGame)
            } else {
                gameList
            }
        }
        .background(Color(hex: "#1a1a2e"))
        .confirmationDialog("Game Select Layout", isPresented: $showLayoutPicker, titleVisibility: .visible) {
            Button("Default (Grid)") { settings.gameSelectLayout = .default }
            Button("3DS Style (Gyro)") { settings.gameSelectLayout = .threeDS }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("Choose how your game library is displayed.")
        }
    }

    private var header: some View {
        HStack {
            Button(action: onDisconnect) {
                HStack(spacing: 4) {
                    Image(systemName: "power")
                        .font(.caption)
                    Text("Disconnect")
                        .font(.caption)
                }
                .padding(.horizontal, 12)
                .padding(.vertical, 8)
                .background(Color(hex: "#e94560"))
                .foregroundColor(.white)
                .cornerRadius(8)
            }

            Spacer()

            Text("Game Library")
                .font(.headline)
                .foregroundColor(.white)

            // Connection type badge
            Text(network.connectionType.rawValue)
                .font(.caption2)
                .fontWeight(.bold)
                .foregroundColor(network.connectionType == .usb ? settings.accentColor : .blue)
                .padding(.horizontal, 8)
                .padding(.vertical, 3)
                .background((network.connectionType == .usb ? settings.accentColor : Color.blue).opacity(0.15))
                .cornerRadius(4)

            Menu {
                Button(action: { showLayoutPicker = true }) {
                    Label("Layouts", systemImage: "square.grid.2x2")
                }
            } label: {
                Image(systemName: "ellipsis.circle")
                    .font(.title3)
                    .foregroundColor(settings.accentColor)
            }
        }
        .padding()
        .background(Color(hex: "#16213e"))
    }

    private var emptyState: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "gamecontroller")
                .font(.system(size: 60))
                .foregroundColor(.gray)
            Text("No games found")
                .font(.title2)
                .foregroundColor(.gray)
            Text("Make sure you've added game folders\non the PC app")
                .font(.subheadline)
                .foregroundColor(.gray.opacity(0.6))
                .multilineTextAlignment(.center)
            Spacer()
        }
    }

    private var gameList: some View {
        VStack(spacing: 0) {
            HStack {
                Image(systemName: "magnifyingglass")
                    .foregroundColor(.gray)
                TextField("Search games...", text: $searchText)
                    .foregroundColor(.white)
            }
            .padding()
            .background(Color(hex: "#16213e"))
            .padding()

            ScrollView {
                LazyVStack(spacing: 8) {
                    ForEach(filteredGames) { game in
                        GameRow(game: game, accentColor: settings.accentColor) {
                            onLaunchGame(game)
                        }
                    }
                }
                .padding(.horizontal)
            }
        }
    }
}

struct GameRow: View {
    let game: GameRom
    let accentColor: Color
    let onPlay: () -> Void

    var body: some View {
        HStack(spacing: 12) {
            ZStack {
                RoundedRectangle(cornerRadius: 8)
                    .fill(Color.orange)
                    .frame(width: 44, height: 44)
                Text("3")
                    .font(.headline)
                    .foregroundColor(.white)
            }

            VStack(alignment: .leading, spacing: 2) {
                Text(game.name)
                    .font(.body)
                    .fontWeight(.semibold)
                    .foregroundColor(.white)
                    .lineLimit(1)
                Text(game.sizeFormatted)
                    .font(.caption)
                    .foregroundColor(.gray)
            }

            Spacer()

            Button(action: onPlay) {
                Text("Play")
                    .font(.subheadline)
                    .fontWeight(.semibold)
                    .padding(.horizontal, 16)
                    .padding(.vertical, 8)
                    .background(accentColor)
                    .foregroundColor(.white)
                    .cornerRadius(8)
            }
        }
        .padding(12)
        .background(Color(hex: "#16213e"))
        .cornerRadius(10)
    }
}
