import SwiftUI

struct SettingsView: View {
    @EnvironmentObject private var settings: AppSettings
    @Environment(\.dismiss) private var dismiss

    private let presetColors: [(name: String, hex: String)] = [
        ("Lime Green", "#32CD32"), ("Blue", "#2196F3"), ("Red", "#F44336"),
        ("Purple", "#9C27B0"), ("Orange", "#FF9800"), ("Pink", "#E91E63"),
        ("Teal", "#009688"), ("Cyan", "#00BCD4"), ("Amber", "#FFC107"),
        ("Deep Purple", "#673AB7"), ("Indigo", "#3F51B5"), ("White", "#FFFFFF")
    ]

    var body: some View {
        NavigationView {
            ZStack {
                Color(hex: "#1a1a2e").ignoresSafeArea()

                ScrollView {
                    VStack(spacing: 24) {
                        Text("Customize")
                            .font(.title2)
                            .fontWeight(.bold)
                            .foregroundColor(.white)

                        RoundedRectangle(cornerRadius: 16)
                            .fill(settings.accentColor)
                            .frame(width: 100, height: 100)
                            .overlay(
                                Image(systemName: "paintpalette.fill")
                                    .font(.title)
                                    .foregroundColor(.white)
                            )

                        LazyVGrid(columns: [GridItem(.adaptive(minimum: 70))], spacing: 12) {
                            ForEach(presetColors, id: \.hex) { preset in
                                Button(action: {
                                    settings.accentColor = Color(hex: preset.hex)
                                }) {
                                    VStack(spacing: 4) {
                                        Circle()
                                            .fill(Color(hex: preset.hex))
                                            .frame(width: 44, height: 44)
                                            .overlay(
                                                Circle()
                                                    .stroke(Color.white.opacity(0.3), lineWidth: settings.accentColor.toHex() == preset.hex ? 3 : 0)
                                            )
                                        Text(preset.name)
                                            .font(.caption2)
                                            .foregroundColor(.gray)
                                    }
                                }
                            }
                        }
                        .padding()

                        Button(action: { dismiss() }) {
                            Text("Done")
                                .fontWeight(.semibold)
                                .frame(maxWidth: .infinity)
                                .padding()
                                .background(settings.accentColor)
                                .foregroundColor(.white)
                                .cornerRadius(12)
                        }
                        .padding(.horizontal)
                    }
                    .padding()
                }
            }
            .navigationBarHidden(true)
        }
    }
}
