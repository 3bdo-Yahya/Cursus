namespace Cursus.PL.Models
{
    /// <summary>
    /// Request body for the <c>POST /Student/SimulateFailure</c> AJAX endpoint.
    /// </summary>
    public class SimulateFailureRequest
    {
        /// <summary>
        /// The primary-key ID of the course to simulate as failed.
        /// </summary>
        public int CourseId { get; set; }
    }
}
