import SwiftUI

enum DPadDirection: Int {
    case up = 0, down = 1, left = 2, right = 3
}

struct DPadView: View {
    let onPress: (DPadDirection) -> Void

    var body: some View {
        VStack(spacing: 4) {
            DPadButton(direction: .up, label: "▲", action: { onPress(.up) })

            HStack(spacing: 4) {
                DPadButton(direction: .left, label: "◀", action: { onPress(.left) })
                Spacer().frame(width: 44)
                DPadButton(direction: .right, label: "▶", action: { onPress(.right) })
            }

            DPadButton(direction: .down, label: "▼", action: { onPress(.down) })
        }
    }
}

struct DPadButton: View {
    let direction: DPadDirection
    let label: String
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(label)
                .font(.title3)
                .foregroundColor(.white)
                .frame(width: 44, height: 44)
                .background(Color(hex: "#333333"))
                .cornerRadius(6)
                .overlay(
                    RoundedRectangle(cornerRadius: 6)
                        .stroke(Color(hex: "#555555"), lineWidth: 1)
                )
        }
    }
}
