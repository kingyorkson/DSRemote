import SwiftUI

struct JoystickView: View {
    let onMove: (Float, Float) -> Void
    @State private var offset = CGSize.zero
    @State private var isDragging = false

    private let maxDistance: CGFloat = 50

    var body: some View {
        ZStack {
            Circle()
                .fill(Color(hex: "#16213e"))
                .overlay(Circle().stroke(Color(hex: "#0f3460"), lineWidth: 2))

            Circle()
                .fill(isDragging ? Color.green : Color(hex: "#32CD32").opacity(0.6))
                .frame(width: 36, height: 36)
                .offset(offset)
        }
        .gesture(
            DragGesture()
                .onChanged { value in
                    isDragging = true
                    let translation = value.translation
                    let distance = sqrt(translation.width * translation.width + translation.height * translation.height)
                    if distance > maxDistance {
                        let scale = maxDistance / distance
                        offset = CGSize(width: translation.width * scale, height: translation.height * scale)
                    } else {
                        offset = translation
                    }
                    let nx = Float(offset.width / maxDistance)
                    let ny = Float(offset.height / maxDistance)
                    onMove(nx, ny)
                }
                .onEnded { _ in
                    isDragging = false
                    offset = .zero
                    onMove(0, 0)
                }
        )
    }
}
