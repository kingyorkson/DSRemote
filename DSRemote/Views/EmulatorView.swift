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
        GeometryReader { geo in
            let isLandscape = geo.size.width > geo.size.height
            let screenHeight = isLandscape ? geo.size.height * 0.85 : geo.size.height * 0.50
            let controlHeight = isLandscape ? geo.size.height * 0.85 : geo.size.height * 0.45

            VStack(spacing: 4) {
                if isLandscape {
                    HStack(spacing: 4) {
                        screensSection(height: screenHeight)
                        controlsSection(height: controlHeight)
                    }
                } else {
                    screensSection(height: screenHeight)
                    controlsSection(height: controlHeight)
                }

                HStack {
                    Button(action: { showPowerAlert = true }) {
                        Image(systemName: "power")
                            .font(.title3)
                            .foregroundColor(.red)
                            .padding(8)
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

                    Button(action: onDisconnect) {
                        HStack(spacing: 4) {
                            Image(systemName: "power")
                            Text("Disconnect")
                                .font(.caption)
                        }
                        .padding(.horizontal, 12)
                        .padding(.vertical, 8)
                        .background(Color(hex: "#e94560"))
                        .foregroundColor(.white)
                        .cornerRadius(8)
                    }
                }
                .padding(.horizontal)
                .padding(.bottom, 8)
            }
        }
        .background(Color(hex: "#1a1a2e"))
        .ignoresSafeArea(.keyboard)
    }

    private func screensSection(height: CGFloat) -> some View {
        VStack(spacing: 4) {
            ScreenView(image: $topScreen, label: "Top Screen")
                .frame(height: height * 0.48)
            ScreenView(image: $bottomScreen, label: "Bottom Screen")
                .frame(height: height * 0.48)
                .overlay(
                    TouchSurfaceView(action: { point in
                        let nx = Float(point.x)
                        let ny = Float(point.y)
                        network.sendInput(.touchDown, args: [nx, ny])
                    })
                )
        }
    }

    private func controlsSection(height: CGFloat) -> some View {
        VStack(spacing: 8) {
            ControlPadView { button in
                network.sendInput(.buttonDown, args: [Float(button.rawValue)])
            } onRelease: { button in
                network.sendInput(.buttonUp, args: [Float(button.rawValue)])
            }

            HStack(spacing: 16) {
                JoystickView { x, y in
                    network.sendInput(.joystickMove, args: [x, y])
                }
                .frame(width: height * 0.25, height: height * 0.25)

                DPadView { direction in
                    network.sendInput(.dPadPress, args: [Float(direction.rawValue)])
                }

                VStack(spacing: 4) {
                    Button("Start") {
                        network.sendInput(.buttonDown, args: [3])
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) {
                            network.sendInput(.buttonUp, args: [3])
                        }
                    }
                    .buttonStyle(ControlButtonStyle(color: settings.accentColor))

                    Button("Select") {
                        network.sendInput(.buttonDown, args: [4])
                        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) {
                            network.sendInput(.buttonUp, args: [4])
                        }
                    }
                    .buttonStyle(ControlButtonStyle(color: settings.accentColor))
                }
            }
        }
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
