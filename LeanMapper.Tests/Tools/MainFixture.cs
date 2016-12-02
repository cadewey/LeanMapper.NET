using System;
using LeanMapper.Tests.Classes;
using Xunit;

namespace LeanMapper.Tests.Tools
{
    public class MainFixture : IDisposable
    {
        /// <summary>
        /// This code will run _once_ before any tests in the MainCollection are run.
        /// </summary>
        public MainFixture()
        {
            Mapper.Config<Parent, DtoParent>()
                .SetDepth(8);
        }


        /// <summary>
        /// This code will run _once_ after all of the tests in the MainCollection have completed.
        /// </summary>
        public void Dispose()
        {
            //
        }
    }

    [CollectionDefinition("MainCollection")]
    public class MainFixtureCollection : ICollectionFixture<MainFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.        
    }
}