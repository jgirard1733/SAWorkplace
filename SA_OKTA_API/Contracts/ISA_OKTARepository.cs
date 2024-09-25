using System.Collections.Generic;

namespace SA_OKTA_API.Contracts
{
    /// <summary>
    /// Interface for DEMOController.cs that defines the methods available for the project.
    /// </summary>
    /// <remarks>
    /// Your project will have a class that inherits from this interface in the Repositories directory, so you need to make sure both are in sync.
    /// </remarks>
    public interface ISAOktaRepository
    {

        /// <summary>
        /// Gets a specific customer from a JSON file.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// A single customer
        /// </returns>
        string UpdateOktaUsers();

    }
}
