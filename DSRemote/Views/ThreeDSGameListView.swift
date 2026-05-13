import SwiftUI

struct ThreeDSGameListView: View {
    @EnvironmentObject private var settings: AppSettings
    let games: [GameRom]
    let onLaunchGame: (GameRom) -> Void

    @State private var currentIndex = 0
    @State private var offset: CGFloat = 0

    var body: some View {
        VStack(spacing: 0) {
            if games.isEmpty {
                emptyState
            } else {
                Spacer()

                // Cartridge area with gyro
                ZStack {
                    RoundedRectangle(cornerRadius: 20)
                        .fill(Color(hex: "#16213e"))
                        .frame(width: 200, height: 280)
                        .overlay(
                            RoundedRectangle(cornerRadius: 20)
                                .stroke(settings.accentColor.opacity(0.3), lineWidth: 2)
                        )

                    VStack(spacing: 12) {
                        Image(systemName: "gamecontroller.fill")
                            .font(.system(size: 60))
                            .foregroundColor(settings.accentColor)

                        RoundedRectangle(cornerRadius: 4)
                            .fill(Color(hex: "#0f3460"))
                            .frame(width: 100, height: 4)

                        // Gyro indicator
                        Circle()
                            .stroke(settings.accentColor.opacity(0.3), lineWidth: 2)
                            .frame(width: 40, height: 40)
                            .overlay(
                                Circle()
                                    .fill(settings.accentColor.opacity(0.15))
                                    .frame(width: 30, height: 30)
                                    .overlay(
                                        Image(systemName: "rotate.right.fill")
                                            .font(.caption)
                                            .foregroundColor(settings.accentColor)
                                    )
                            )
                    }
                }
                .overlay(
                    Text(games[currentIndex].platform == "ThreeDS" ? "3DS" : "DS")
                        .font(.caption)
                        .fontWeight(.bold)
                        .foregroundColor(settings.accentColor)
                        .padding(.horizontal, 10)
                        .padding(.vertical, 4)
                        .background(Color(hex: "#1a1a2e"))
                        .cornerRadius(6)
                        .offset(y: -140),
                    alignment: .center
                )
                .padding(.bottom, 24)

                // Game name
                Text(games[currentIndex].name)
                    .font(.title3)
                    .fontWeight(.bold)
                    .foregroundColor(.white)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal, 40)
                    .padding(.bottom, 8)

                Text(games[currentIndex].sizeFormatted)
                    .font(.caption)
                    .foregroundColor(.gray)

                Spacer()

                // Navigation arrows
                HStack(spacing: 40) {
                    Button(action: prevGame) {
                        Image(systemName: "chevron.left.circle.fill")
                            .font(.system(size: 44))
                            .foregroundColor(currentIndex > 0 ? settings.accentColor : .gray.opacity(0.3))
                    }
                    .disabled(currentIndex == 0)

                    Button(action: { onLaunchGame(games[currentIndex]) }) {
                        Text("Play")
                            .font(.headline)
                            .fontWeight(.bold)
                            .padding(.horizontal, 40)
                            .padding(.vertical, 14)
                            .background(settings.accentColor)
                            .foregroundColor(.white)
                            .cornerRadius(12)
                    }

                    Button(action: nextGame) {
                        Image(systemName: "chevron.right.circle.fill")
                            .font(.system(size: 44))
                            .foregroundColor(currentIndex < games.count - 1 ? settings.accentColor : .gray.opacity(0.3))
                    }
                    .disabled(currentIndex == games.count - 1)
                }
                .padding(.bottom, 40)
            }
        }
        .background(Color(hex: "#1a1a2e"))
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
            Spacer()
        }
    }

    private func prevGame() {
        withAnimation(.easeInOut(duration: 0.3)) {
            currentIndex = max(0, currentIndex - 1)
        }
    }

    private func nextGame() {
        withAnimation(.easeInOut(duration: 0.3)) {
            currentIndex = min(games.count - 1, currentIndex + 1)
        }
    }
}
