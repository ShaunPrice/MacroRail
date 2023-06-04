using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroRail
{
    // Function to calculate Depth of Field for Macro Photography
    internal class CameraCalcs
    {
        double m_focalLength = 0;
        double m_extensionLength = 0;
        double m_aperture = 0;
        double m_circleOfConfusion = 0;
        double m_subjectDistance = 0;
        double m_subjectDepth = 0;

        double m_nearDistance = 0;
        double m_farDistance = 0;
        double m_hyperfocalDistance = 0;
        double m_depthOfField = 0;

        double m_overlapPercent = 50; // 50% overlap
        double m_shotsRequired = 1;
        double m_step_size = 0;

        public CameraCalcs()
        {
            m_focalLength = 0;
            m_aperture = 0;
            m_circleOfConfusion = 0;
            m_subjectDistance = 0;
            m_extensionLength = 0;

            m_nearDistance = 0;
            m_farDistance = 0;
            m_hyperfocalDistance = 0;
            m_depthOfField = 0;
        }

        public CameraCalcs(double focalLength, double externsionLength, double aperture, double circleOfConfusion, double subjectDistance, double subjectDepth)
        {
            m_focalLength = focalLength;
            m_extensionLength = externsionLength;
            m_aperture = aperture;
            m_circleOfConfusion = circleOfConfusion;
            m_subjectDistance = subjectDistance;
            m_subjectDepth = subjectDepth;

            m_nearDistance = 0;
            m_farDistance = 0;
            m_hyperfocalDistance = 0;
            m_depthOfField = 0;
        }
        public double ShotsRequired
        {
            get
            {
                return m_shotsRequired;
            }
            set
            {
                m_shotsRequired = value;
            }
        }

        public double FocalLength
        {
            get
            {
                return m_focalLength;
            }
            set
            {
                m_focalLength = value;
            }
        }

        public double Aperture
        {
            get
            {
                return m_aperture;
            }
            set
            { 
                m_aperture = value; 
            }
        }
        public double CircleOfConfusion
        {
            get
            {
                return m_circleOfConfusion;
            }
            set
            {
                m_circleOfConfusion = value;
            }
        }
        public double SubjectDistance
        {
            get
            {
                return m_subjectDistance;
            }
            set
            {
                m_subjectDistance = value;
            }
        }

        public double Extension_tube_length
        {
            get
            {
                return m_extensionLength;
            }
            set
            {
                m_extensionLength = value;
            }
        }

        public double SubjectDepth
        {
            get
            {
                return m_subjectDepth;
            }
            set
            {
                m_subjectDepth = value;
            }
        }

        public double StepSize
        {
            get
            {
                return m_step_size;
            }
        }

        public double OverlapPercent
        {
            get
            {
                return m_overlapPercent;
            }
            set
            {
                m_overlapPercent = value;
            }
        }

        public double NearDistance
        {
            get
            {
                return m_nearDistance;
            }
        }

        public double FarDistance
        {
           get
            {
                return m_farDistance;
            }
        }

        public double HyperfocalDistance
        {
            get
            {
                return m_hyperfocalDistance;
            }
        }

        public double DepthOfField
        {
            get
            {
                return m_depthOfField;
            }
        }

        public bool Calculate()
        {
            if (m_focalLength == 0 || m_aperture == 0 || m_circleOfConfusion == 0 || m_subjectDistance == 0 || m_subjectDepth == 0)
            {
                return false;
            }
            else
            {
                Calculate(m_focalLength, m_extensionLength, m_aperture, m_circleOfConfusion, m_subjectDistance, m_subjectDepth);

                return true;
            }
        }

        /// <summary>
        /// Calculates the number of shots required to cover the subject
        /// </summary>
        /// <param name="focalLength">Focal length of the lens</param>
        /// <param name="aperture">Aperture of the camera</param>
        /// <param name="circleOfConfusion">Circle of Confusion of the camera</param>
        /// <param name="subjectDistance">Distance to the subject from the front of the lense</param>
        /// <param name="extensionTubeLength">Length of the extension tube</param>
        /// <returns>True if successful</returns>
        public bool Calculate(double focalLength, double extensionLength, double aperture, double circleOfConfusion, double subjectDistance, double subjectDepth)
        {
            if (focalLength == 0 || aperture == 0 || circleOfConfusion == 0 || subjectDistance == 0 || subjectDepth == 0)
            {
                return false;
            }
            else
            {
                m_focalLength = focalLength;
                m_extensionLength = extensionLength;
                m_aperture = aperture;
                m_circleOfConfusion = circleOfConfusion;
                m_subjectDistance = subjectDistance;
                m_subjectDepth = subjectDepth;

                double magnification = extensionLength / focalLength;

                // Corrected subject distance
                double l_subjectDistance = (m_subjectDistance + m_focalLength + m_extensionLength) / (1 + m_circleOfConfusion);

                m_hyperfocalDistance = (m_focalLength * m_focalLength) / (m_aperture * circleOfConfusion);

                if (m_hyperfocalDistance > (l_subjectDistance - m_focalLength))
                {
                    m_nearDistance = (m_hyperfocalDistance * l_subjectDistance) / (m_hyperfocalDistance + (l_subjectDistance - m_focalLength));
                    m_farDistance = (m_hyperfocalDistance * l_subjectDistance) / (m_hyperfocalDistance - (l_subjectDistance - m_focalLength));
                }
                else
                {
                    m_nearDistance = 0;
                    m_farDistance = 2 * m_hyperfocalDistance * l_subjectDistance / (m_hyperfocalDistance + l_subjectDistance);
                }

                m_depthOfField = m_farDistance - m_nearDistance;

                m_shotsRequired = Math.Ceiling(m_subjectDepth / (m_depthOfField * (1 - (m_overlapPercent/100))));
                m_step_size = m_subjectDepth / m_shotsRequired;

                return true;
            }
        }
    }
}
