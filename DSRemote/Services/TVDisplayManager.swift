import UIKit

@MainActor
class TVDisplayManager: ObservableObject {
    @Published var isTVActive = false

    private var externalWindow: UIWindow?
    private var tvViewController: TVViewController?

    override init() {
        super.init()
        NotificationCenter.default.addObserver(
            self, selector: #selector(screenDidConnect),
            name: UIScreen.didConnectNotification, object: nil)
        NotificationCenter.default.addObserver(
            self, selector: #selector(screenDidDisconnect),
            name: UIScreen.didDisconnectNotification, object: nil)

        if UIScreen.screens.count > 1 {
            setupExternalDisplay(screen: UIScreen.screens[1])
        }
    }

    deinit {
        NotificationCenter.default.removeObserver(self)
    }

    func updateTopImage(_ image: UIImage?) {
        tvViewController?.topImageView.image = image
    }

    func updateBottomImage(_ image: UIImage?) {
        tvViewController?.bottomImageView.image = image
    }

    func deactivate() {
        externalWindow?.isHidden = true
        externalWindow = nil
        tvViewController = nil
        isTVActive = false
    }

    @objc private func screenDidConnect(_ notification: Notification) {
        guard let newScreen = notification.object as? UIScreen else { return }
        setupExternalDisplay(screen: newScreen)
    }

    @objc private func screenDidDisconnect(_: Notification) {
        deactivate()
    }

    private func setupExternalDisplay(screen: UIScreen) {
        let screenSize = screen.bounds
        let vc = TVViewController()
        vc.view.frame = screenSize

        let window = UIWindow(frame: screenSize)
        window.screen = screen
        window.rootViewController = vc
        window.isHidden = false
        window.clipsToBounds = true

        externalWindow = window
        tvViewController = vc
        isTVActive = true
    }
}

private class TVViewController: UIViewController {
    let topImageView: UIImageView = {
        let iv = UIImageView()
        iv.contentMode = .scaleAspectFit
        iv.clipsToBounds = true
        return iv
    }()

    let bottomImageView: UIImageView = {
        let iv = UIImageView()
        iv.contentMode = .scaleAspectFit
        iv.clipsToBounds = true
        return iv
    }()

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .black

        topImageView.translatesAutoresizingMaskIntoConstraints = false
        bottomImageView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(topImageView)
        view.addSubview(bottomImageView)

        NSLayoutConstraint.activate([
            topImageView.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            topImageView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 20),
            topImageView.widthAnchor.constraint(equalTo: view.widthAnchor, multiplier: 0.6),
            topImageView.heightAnchor.constraint(equalTo: view.heightAnchor, multiplier: 0.40),

            bottomImageView.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            bottomImageView.bottomAnchor.constraint(equalTo: view.safeAreaLayoutGuide.bottomAnchor, constant: -20),
            bottomImageView.widthAnchor.constraint(equalTo: view.widthAnchor, multiplier: 0.6),
            bottomImageView.heightAnchor.constraint(equalTo: view.heightAnchor, multiplier: 0.35),
        ])
    }
}
