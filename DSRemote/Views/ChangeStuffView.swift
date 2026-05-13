import SwiftUI

struct ChangeStuffView: View {
    @EnvironmentObject private var settings: AppSettings
    @EnvironmentObject private var layoutService: LayoutService
    @Environment(\.dismiss) private var dismiss

    @State private var showInGameEditor = false
    @State private var showGameSelectStyles = false

    var body: some View {
        NavigationView {
            ZStack {
                Color(hex: "#1a1a2e").ignoresSafeArea()

                VStack(spacing: 24) {
                    Text("Change Stuff")
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundColor(.white)
                        .padding(.top, 20)

                    Text("Customize your experience")
                        .font(.subheadline)
                        .foregroundColor(.gray)

                    Button(action: { showInGameEditor = true }) {
                        optionCard(
                            icon: "gamecontroller",
                            title: "In-Game Controls",
                            subtitle: "Edit button positions, colors, and sizes for the emulator touch controls"
                        )
                    }

                    Button(action: { showGameSelectStyles = true }) {
                        optionCard(
                            icon: "square.grid.2x2",
                            title: "Game Select Screen",
                            subtitle: "Switch between Default grid layout and 3DS-style game browser"
                        )
                    }

                    Spacer()

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
        .fullScreenCover(isPresented: $showInGameEditor) {
            LayoutEditorView(layout: layoutService.activeLayout)
                .environmentObject(settings)
                .environmentObject(layoutService)
        }
        .sheet(isPresented: $showGameSelectStyles) {
            GameSelectStyleView()
                .environmentObject(settings)
        }
    }

    private func optionCard(icon: String, title: String, subtitle: String) -> some View {
        HStack(spacing: 16) {
            ZStack {
                RoundedRectangle(cornerRadius: 14)
                    .fill(settings.accentColor.opacity(0.15))
                    .frame(width: 56, height: 56)
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundColor(settings.accentColor)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text(title)
                    .font(.headline)
                    .foregroundColor(.white)
                Text(subtitle)
                    .font(.caption)
                    .foregroundColor(.gray)
                    .multilineTextAlignment(.leading)
            }

            Spacer()

            Image(systemName: "chevron.right")
                .foregroundColor(.gray)
        }
        .padding(16)
        .background(Color(hex: "#16213e"))
        .cornerRadius(14)
    }
}
