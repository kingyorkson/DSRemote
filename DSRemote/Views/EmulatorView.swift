import SwiftUI

struct EmulatorView: View {
    @EnvironmentObject private var network: NetworkService
    @EnvironmentObject private var settings: AppSettings
    @EnvironmentObject private var layoutService: LayoutService
    @Binding var topScreen: UIImage?
    @Binding var bottomScreen: UIImage?
    let onPowerOff: () -> Void
    let onDisconnect: () -> Void

    @State private var showPowerAlert = false

    var body: some View {
        VStack(spacing: 4) {
            // Top bar
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
                        network.sendStopEmulation()
                        onPowerOff()
                    }
                } message: {
                    Text("This will stop the emulator and return to game selection.")
                }

                Spacer()
                Text("DSRemote").font(.caption).foregroundColor(.gray)
                Spacer()

                Button(action: onDisconnect) {
                    HStack(spacing: 3) {
                        Image(systemName: "power").font(.caption2)
                        Text("Disconnect").font(.caption2)
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 6)
                    .background(Color(hex: "#e94560"))
                    .foregroundColor(.white)
                    .cornerRadius(8)
                }
            }
            .padding(.horizontal, 8)
            .padding(.top, 2)

            // Top screen
            ScreenView(image: $topScreen, label: "Top")
                .frame(height: UIScreen.main.bounds.height * 0.38)

            // Bottom screen with touch overlay
            ScreenView(image: $bottomScreen, label: "Bottom")
                .frame(height: UIScreen.main.bounds.height * 0.26)
                .overlay(TouchSurfaceView(
                    onTouchDown: { pt in network.sendInput(.touchDown, args: [Float(pt.x), Float(pt.y)]) },
                    onTouchMove: { pt in network.sendInput(.touchMove, args: [Float(pt.x), Float(pt.y)]) },
                    onTouchUp: { network.sendInput(.touchUp, args: []) }
                ))

            // Button controls from active layout
            GeometryReader { geo in
                ZStack {
                    ForEach(layoutService.activeLayout.buttons) { btn in
                        controlButton(btn, containerSize: geo.size)
                    }
                }
            }
            .padding(.horizontal, 8)
            .padding(.bottom, 4)
        }
        .background(Color(hex: "#1a1a2e"))
        .background(settings.accentColor.opacity(0.08))
        .ignoresSafeArea(.keyboard)
    }

    private func controlButton(_ config: ButtonConfig, containerSize: CGSize) -> some View {
        let w = containerSize.width * config.relSize
        let h = containerSize.height * config.relSize

        return Group {
            if config.id == "dpad" {
                compactDPad(size: min(w, h) * 0.8)
            } else if config.id == "joystick" {
                JoystickView { x, y in
                    network.sendInput(.joystickMove, args: [x, y])
                }
                .frame(width: min(w, h), height: min(w, h))
            } else {
                let btnId = buttonId(for: config.id)
                Button(action: {}) {
                    Text(config.label)
                        .font(.system(size: max(10, config.relSize * 30)))
                        .fontWeight(.bold)
                        .foregroundColor(.white)
                        .frame(width: min(w, h), height: min(w, h))
                        .background(config.color.opacity(0.3))
                        .clipShape(Circle())
                        .overlay(Circle().stroke(config.color, lineWidth: 2))
                }
                .simultaneousGesture(
                    DragGesture(minimumDistance: 0)
                        .onChanged { _ in network.sendInput(.buttonDown, args: [Float(btnId)]) }
                        .onEnded { _ in network.sendInput(.buttonUp, args: [Float(btnId)]) }
                )
            }
        }
        .position(x: config.relX * containerSize.width, y: config.relY * containerSize.height)
    }

    private func compactDPad(size: CGFloat) -> some View {
        VStack(spacing: 2) {
            CompactDPadBtn(direction: .up, label: "\u{25B2}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [0]) })
            HStack(spacing: 2) {
                CompactDPadBtn(direction: .left, label: "\u{25C0}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [2]) })
                Color.clear.frame(width: size * 0.3, height: size * 0.3)
                CompactDPadBtn(direction: .right, label: "\u{25B6}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [3]) })
            }
            CompactDPadBtn(direction: .down, label: "\u{25BC}", size: size * 0.3, action: { network.sendInput(.dPadPress, args: [1]) })
        }
    }

    private func buttonId(for id: String) -> Int {
        switch id {
        case "a": return 0
        case "b": return 1
        case "x": return 2
        case "y": return 3
        case "l": return 4
        case "r": return 5
        case "start": return 6
        case "select": return 7
        default: return 0
        }
    }
}

struct CompactDPadBtn: View {
    let direction: DPadDirection
    let label: String
    let size: CGFloat
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(label)
                .font(.system(size: size * 0.5))
                .foregroundColor(.white)
                .frame(width: size, height: size)
                .background(Color(hex: "#333333"))
                .cornerRadius(4)
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
                    Image(systemName: "rectangle.split.2x1").font(.largeTitle).foregroundColor(.gray)
                    Text(label).font(.caption).foregroundColor(.gray)
                }
            }
        }
        .cornerRadius(8)
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(hex: "#0f3460"), lineWidth: 1))
        .padding(.horizontal, 4)
    }
}

struct TouchSurfaceView: UIViewRepresentable {
    let onTouchDown: (CGPoint) -> Void
    let onTouchMove: (CGPoint) -> Void
    let onTouchUp: () -> Void

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
        Coordinator(onTouchDown: onTouchDown, onTouchMove: onTouchMove, onTouchUp: onTouchUp)
    }

    class Coordinator: NSObject {
        let onTouchDown: (CGPoint) -> Void
        let onTouchMove: (CGPoint) -> Void
        let onTouchUp: () -> Void

        init(onTouchDown: @escaping (CGPoint) -> Void, onTouchMove: @escaping (CGPoint) -> Void, onTouchUp: @escaping () -> Void) {
            self.onTouchDown = onTouchDown
            self.onTouchMove = onTouchMove
            self.onTouchUp = onTouchUp
        }

        private func normalizedLocation(in view: UIView, from gesture: UIGestureRecognizer) -> CGPoint {
            let point = gesture.location(in: view)
            return CGPoint(x: point.x / view.bounds.width, y: point.y / view.bounds.height)
        }

        @objc func handlePan(_ gesture: UIPanGestureRecognizer) {
            guard let view = gesture.view else { return }
            let pt = normalizedLocation(in: view, from: gesture)
            switch gesture.state {
            case .began: onTouchDown(pt)
            case .changed: onTouchMove(pt)
            case .ended, .cancelled: onTouchUp()
            default: break
            }
        }

        @objc func handleTap(_ gesture: UITapGestureRecognizer) {
            guard let view = gesture.view else { return }
            let pt = normalizedLocation(in: view, from: gesture)
            onTouchDown(pt)
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { self.onTouchUp() }
        }
    }
}
