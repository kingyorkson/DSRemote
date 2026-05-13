import SwiftUI

struct GameSelectStyleView: View {
    @EnvironmentObject private var settings: AppSettings
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationView {
            ZStack {
                Color(hex: "#1a1a2e").ignoresSafeArea()

                VStack(spacing: 24) {
                    Text("Game Select Style")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundColor(.white)
                        .padding(.top, 20)

                    Text("Choose how your game library looks")
                        .font(.subheadline)
                        .foregroundColor(.gray)

                    Button(action: {
                        settings.gameSelectLayout = .default
                    }) {
                        styleCard(
                            isSelected: settings.gameSelectLayout == .default,
                            icon: "square.grid.2x2",
                            title: "Default (Grid)",
                            description: "Classic grid layout with search — shows all games at once"
                        )
                    }

                    Button(action: {
                        settings.gameSelectLayout = .threeDS
                    }) {
                        styleCard(
                            isSelected: settings.gameSelectLayout == .threeDS,
                            icon: "rotate.3d",
                            title: "3DS Style",
                            description: "Cartridge view with gyro — one game at a time with scroll animation"
                        )
                    }

                    Spacer()

                    Text("Changes apply immediately")
                        .font(.caption)
                        .foregroundColor(.gray)

                    Button(action: { dismiss() }) {
                        Text("Done")
                            .fontWeight(.semibold)
                            .frame(maxWidth: 300)
                            .padding()
                            .background(settings.accentColor)
                            .foregroundColor(.white)
                            .cornerRadius(12)
                    }
                    .padding(.bottom, 20)
                }
                .padding(.horizontal, 24)
            }
            .navigationBarHidden(true)
        }
    }

    private func styleCard(isSelected: Bool, icon: String, title: String, description: String) -> some View {
        HStack(spacing: 16) {
            ZStack {
                RoundedRectangle(cornerRadius: 14)
                    .fill(isSelected ? settings.accentColor : Color(hex: "#16213e"))
                    .frame(width: 56, height: 56)
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundColor(isSelected ? .white : settings.accentColor)
            }

            VStack(alignment: .leading, spacing: 4) {
                HStack(spacing: 8) {
                    Text(title)
                        .font(.headline)
                        .foregroundColor(.white)
                    if isSelected {
                        Text("Active")
                            .font(.caption2)
                            .fontWeight(.bold)
                            .foregroundColor(settings.accentColor)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 3)
                            .background(settings.accentColor.opacity(0.2))
                            .cornerRadius(4)
                    }
                }
                Text(description)
                    .font(.caption)
                    .foregroundColor(.gray)
                    .multilineTextAlignment(.leading)
            }

            Spacer()

            if isSelected {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundColor(settings.accentColor)
                    .font(.title3)
            }
        }
        .padding(16)
        .background(Color(hex: "#16213e"))
        .cornerRadius(14)
        .overlay(
            RoundedRectangle(cornerRadius: 14)
                .stroke(isSelected ? settings.accentColor : Color.clear, lineWidth: 2)
        )
    }
}
