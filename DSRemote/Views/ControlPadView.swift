import SwiftUI

enum GameButton: Int {
    case a = 0, b = 1, x = 2, y = 3, l = 4, r = 5, start = 6, select = 7
}

struct ControlPadView: View {
    let onPress: (GameButton) -> Void
    let onRelease: (GameButton) -> Void

    var body: some View {
        HStack(spacing: 20) {
            VStack(spacing: 8) {
                CircleButton(label: "Y", color: .yellow, onPress: { onPress(.y) }, onRelease: { onRelease(.y) })
                HStack(spacing: 20) {
                    CircleButton(label: "X", color: .blue, onPress: { onPress(.x) }, onRelease: { onRelease(.x) })
                    CircleButton(label: "B", color: .red, onPress: { onPress(.b) }, onRelease: { onRelease(.b) })
                }
                CircleButton(label: "A", color: .green, onPress: { onPress(.a) }, onRelease: { onRelease(.a) })
            }

            HStack(spacing: 12) {
                Button("L") {
                    onPress(.l)
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { onRelease(.l) }
                }
                .buttonStyle(ShoulderButtonStyle())

                Button("R") {
                    onPress(.r)
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { onRelease(.r) }
                }
                .buttonStyle(ShoulderButtonStyle())
            }
        }
    }
}

struct CircleButton: View {
    let label: String
    let color: Color
    let onPress: () -> Void
    let onRelease: () -> Void

    var body: some View {
        Button(action: {}) {
            Text(label)
                .font(.title3)
                .fontWeight(.bold)
                .foregroundColor(.white)
                .frame(width: 48, height: 48)
                .background(color.opacity(0.3))
                .clipShape(Circle())
                .overlay(Circle().stroke(color, lineWidth: 2))
        }
        .simultaneousGesture(
            DragGesture(minimumDistance: 0)
                .onChanged { _ in onPress() }
                .onEnded { _ in onRelease() }
        )
    }
}

struct ShoulderButtonStyle: ButtonStyle {
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.caption)
            .fontWeight(.bold)
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .background(configuration.isPressed ? Color.gray : Color(hex: "#333333"))
            .foregroundColor(.white)
            .cornerRadius(8)
            .overlay(
                RoundedRectangle(cornerRadius: 8)
                    .stroke(Color(hex: "#555555"), lineWidth: 1)
            )
    }
}
