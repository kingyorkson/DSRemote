import SwiftUI

struct EmulatorView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @Binding var topScreen: UIImage?
    @Binding var bottomScreen: UIImage?
    let onPowerOff: () -> Void
    let onDisconnect: () -> Void

    @State private var showPowerAlert = false

    var body: some View {
        VStack(spacing: 6) {
            // Power + Disconnect bar
            HStack {
                Button(action: { showPowerAlert = true }) {
                    Image(systemName: "power")
                        .font(.caption)
                        .foregroundColor(.red)
                        .padding(6)
                        .background(Color.red.opacity(0.15))
                        .clipShape(Circle())
                }
                .alert("Quit Game?", isPresented: $showPowerAlert) {
                    Button("Cancel", role: .cancel) {}
                    Button("Quit", role: .destructive) {
                        network.sendInput(.buttonDown, args: [99])
                        onPowerOff()
                    }
                } message: {
                    Text("This will save and return to game selection.")
                }

                Spacer()

                Text("DSRemote")
                    .font(.caption)
                    .foregroundColor(.gray)

                Spacer()

                Button(action: onDisconnect) {
                    HStack(spacing: 3) {
                        Image(systemName: "power")
                            .font(.caption2)
                        Text("Disconnect")
                            .font(.caption2)
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 6)
                    .background(Color(hex: "#e94560"))
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
            }
            .padding(.horizontal, 8)
            .padding(.top, 4)

            // Top screen
            ScreenView(image: $topScreen, label: "Top Screen")
                .frame(height: UIScreen.main.bounds.height * 0.32)

            // Bottom screen
            ScreenView(image: $bottomScreen, label: "Bottom Screen")
                .frame(height: UIScreen.main.bounds.height * 0.22)
                .overlay(
                    TouchSurfaceView(action: { point in
                        network.sendInput(.touchDown, args: [Float(point.x), Float(point.y)])
                    })
                )

            // 3DS-style controls
            VStack(spacing: 10) {
                // L / R shoulder buttons
                HStack(spacing: 20) {
                    ShoulderLabel(label: "L", onPress: {
                        network.sendInput(.buttonDown, args: [4])
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { network.sendInput(.buttonUp, args: [4]) }
                    })
                    Spacer()
                    ShoulderLabel(label: "R", onPress: {
                        network.sendInput(.buttonDown, args: [5])
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { network.sendInput(.buttonUp, args: [5]) }
                    })
                }
                .padding(.horizontal, 20)

                // D-Pad (left) + ABXY (right)
                HStack(alignment: .center, spacing: 0) {
                    // D-Pad
                    DPadView { direction in
                        network.sendInput(.dPadPress, args: [Float(direction.rawValue)])
                    }
                    .frame(maxWidth: .infinity)

                    Spacer()

                    // ABXY diamond
                    VStack(spacing: 6) {
                        CircleButton(label: "Y", color: .yellow, onPress: { network.sendInput(.buttonDown, args: [3]) }, onRelease: { network.sendInput(.buttonUp, args: [3]) })
                        HStack(spacing: 16) {
                            CircleButton(label: "X", color: .blue, onPress: { network.sendInput(.buttonDown, args: [2]) }, onRelease: { network.sendInput(.buttonUp, args: [2]) })
                            CircleButton(label: "B", color: .red, onPress: { network.sendInput(.buttonDown, args: [1]) }, onRelease: { network.sendInput(.buttonUp, args: [1]) })
                        }
                        CircleButton(label: "A", color: .green, onPress: { network.sendInput(.buttonDown, args: [0]) }, onRelease: { network.sendInput(.buttonUp, args: [0]) })
                    }
                    .frame(maxWidth: .infinity)
                }
                .padding(.horizontal, 16)

                // Joystick + Start/Select
                HStack(alignment: .center, spacing: 0) {
                    JoystickView { x, y in
                        network.sendInput(.joystickMove, args: [x, y])
                    }
                    .frame(width: 70, height: 70)
                    .frame(maxWidth: .infinity)

                    Spacer()

                    HStack(spacing: 20) {
                        Button("Select") {
                            network.sendInput(.buttonDown, args: [6])
                            DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { network.sendInput(.buttonUp, args: [6]) }
                        }
                        .buttonStyle(ControlButtonStyle(color: settings.accentColor))

                        Button("Start") {
                            network.sendInput(.buttonDown, args: [7])
                            DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { network.sendInput(.buttonUp, args: [7]) }
                        }
                        .buttonStyle(ControlButtonStyle(color: settings.accentColor))
                    }
                    .frame(maxWidth: .infinity)
                }
                .padding(.horizontal, 16)
            }
            .padding(.bottom, 8)
        }
        .background(Color(hex: "#1a1a2e"))
        .ignoresSafeArea(.keyboard)
    }
}

struct ScreenView: View {
    @Binding var image: UIImage?
    let label: String

    var body: some View {
        ZStack {
            Color.black
            if let img = image {
                Image(uiImage: img)
                    .resizable()
                    .aspectRatio(contentMode: .fit)
            } else {
                VStack {
                    Image(systemName: "rectangle.split.2x1")
                        .font(.largeTitle)
                        .foregroundColor(.gray)
                    Text(label)
                        .font(.caption)
                        .foregroundColor(.gray)
                }
            }
        }
        .cornerRadius(8)
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(Color(hex: "#0f3460"), lineWidth: 1)
        )
        .padding(.horizontal, 4)
    }
}

struct TouchSurfaceView: UIViewRepresentable {
    let action: (CGPoint) -> Void

    func makeUIView(context: Context) -> UIView {
        let view = UIView()
        view.backgroundColor = .clear
        let pan = UIPanGestureRecognizer(target: context.coordinator, action: #selector(Coordinator.handlePan(_:)))
        let tap = UITapGestureRecognizer(target: context.coordinator, action: #selector(Coordinator.handleTap(_:)))
        pan.maximumNumberOfTouches = 1
        view.addGestureRecognizer(pan)
        view.addGestureRecognizer(tap)
        return view
    }

    func updateUIView(_ uiView: UIView, context: Context) {}

    func makeCoordinator() -> Coordinator {
        Coordinator(action: action)
    }

    class Coordinator: NSObject {
        let action: (CGPoint) -> Void
        init(action: @escaping (CGPoint) -> Void) { self.action = action }

        @objc func handlePan(_ gesture: UIPanGestureRecognizer) {
            guard let view = gesture.view else { return }
            let point = gesture.location(in: view)
            let normalized = CGPoint(x: point.x / view.bounds.width, y: point.y / view.bounds.height)
            action(normalized)
        }

        @objc func handleTap(_ gesture: UITapGestureRecognizer) {
            guard let view = gesture.view else { return }
            let point = gesture.location(in: view)
            let normalized = CGPoint(x: point.x / view.bounds.width, y: point.y / view.bounds.height)
            action(normalized)
        }
    }
}

struct ControlButtonStyle: ButtonStyle {
    let color: Color
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.caption2)
            .fontWeight(.bold)
            .padding(.horizontal, 8)
            .padding(.vertical, 6)
            .background(configuration.isPressed ? color.opacity(0.8) : color.opacity(0.3))
            .foregroundColor(.white)
            .cornerRadius(6)
            .overlay(
                RoundedRectangle(cornerRadius: 6)
                    .stroke(color, lineWidth: 1)
            )
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
                .frame(width: 46, height: 46)
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

struct ShoulderLabel: View {
    let label: String
    let onPress: () -> Void

    var body: some View {
        Button(action: onPress) {
            Text(label)
                .font(.caption)
                .fontWeight(.bold)
                .foregroundColor(.white)
                .frame(width: 60, height: 28)
                .background(Color(hex: "#333333"))
                .cornerRadius(6)
                .overlay(
                    RoundedRectangle(cornerRadius: 6)
                        .stroke(Color(hex: "#555555"), lineWidth: 1)
                )
        }
    }
}
