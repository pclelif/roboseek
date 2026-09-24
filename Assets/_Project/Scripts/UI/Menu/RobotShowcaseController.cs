using UnityEngine;
using UnityEngine.UI;
using Robot.Robots.Customization;
namespace Robot.UI.Production
{
    public sealed class RobotShowcaseController : MonoBehaviour
    {
        public RobotColorCustomizer robot;
        public TargetVisualLibrary visuals;
        public RawImage menuImage;
        public RawImage hubImage, resultImage;
        public GameObject resultPose;
        public void Refresh()
        {
            if (robot == null || visuals == null || (menuImage == null && hubImage == null && resultImage == null)) return;
            visuals.Clear();
            var texture = visuals.CreatePreview(robot.gameObject, 20, true);
            if (menuImage != null) menuImage.texture = texture;
            if (hubImage != null) hubImage.texture = texture;
            if (resultImage != null) resultImage.texture = resultPose != null ? visuals.CreatePreview(resultPose, 21, true, robot.gameObject) : texture;
        }
    }
}
