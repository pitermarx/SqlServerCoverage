using FluentAssertions;
using SqlServerCoverage.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace SqlServerCoverage.Tests
{
    [UsesVerify]
    public class Tests : TestsBase
    {
        [Fact]
        public void DatabaseDoesNotExist()
        {
            Action a = () => NewCoverageController().NewSession();
            a.Should().Throw<InvalidOperationException>()
                .WithMessage("Can't start session on database SqlServerCoverageTest because it does not exist.");
        }

        [Fact]
        public void SessionDoesNotExist()
        {
            WithDatabase(() =>
            {
                Action a = () => NewCoverageController().AttachSession();
                a.Should().Throw<InvalidOperationException>()
                    .WithMessage("Can't attach to session SqlServerCoverageTrace-* because it does not exist.");
            });
        }

        [Fact]
        public void StartStopTrace()
        {
            WithNewSession(session =>
            {
                session.SessionName.Should().StartWith("SqlServerCoverageTrace-");
            });
        }

        [Fact]
        public void CollectNoObjects()
        {
            WithNewSession(session =>
            {
                var result = session.ReadCoverage(false);
                result.SourceObjects.Should().BeEmpty();
                result.CoveragePercent.Should().Be(0);
            });
        }

        [Fact]
        public void Collect1Sproc()
        {
            WithTraceAndSproc(session =>
            {
                var result = session.ReadCoverage(false);
                var obj = result.SourceObjects.Should().ContainSingle().Subject;
                obj.Name.Should().Be("[dbo].[TestProcedureForCoverage]");
                obj.CoveragePercent.Should().Be(0);
                obj.IsCovered.Should().Be(false);
                obj.Statements.Where(s => s.HitCount == 0).Should().HaveCount(3);
            });
        }

        [Fact]
        public void Collect1SprocWithCoverage()
        {
            WithTraceAndSproc(session =>
            {
                Execute("EXEC TestProcedureForCoverage 1", c => c.ExecuteScalar());
                Execute("EXEC TestProcedureForCoverage 1", c => c.ExecuteScalar());
                var result = session.ReadCoverage();

                var obj = result.SourceObjects.Should().ContainSingle().Subject;
                obj.Name.Should().Be("[dbo].[TestProcedureForCoverage]");
                obj.Type.Should().Be(SourceObjectType.Procedure);
                obj.Text.Should().Be(SprocText);
                obj.IsCovered.Should().Be(true);
                obj.StatementCount.Should().Be(3);
                obj.CoveredStatementCount.Should().Be(2);
                obj.CoveragePercent.Should().Be((2.0/3) * 100);
                obj.Statements.Where(s => s.HitCount == 2).Should().HaveCount(2);
                obj.Statements
                    .Where(s => s.HitCount == 0)
                    .Should().ContainSingle()
                    .Subject.Text.Trim().Should().Be("SELECT 20");

                Execute("EXEC TestProcedureForCoverage 2", c => c.ExecuteScalar());
                session.ReadCoverage().CoveragePercent.Should().Be(100);
            });
        }

        [Fact]
        public Task TestSonarOutput()
        {
            string xml = null;

            WithTestData(session =>
            {
                var result = session.ReadCoverage();
                xml = result.GetSonarGenericXml();
            });

            return Verifier.Verify(xml, VerifySettings());
        }

        [Fact]
        public Task TestHtmlOutput()
        {
            string html = null;

            WithTestData(session =>
            {
                var result = session.ReadCoverage();
                html = result.GetHtml();
            });

            return Verifier.Verify(html, VerifySettings());
        }

        [Fact]
        public Task TestOpenCoverOutput()
        {
            string xml = null;

            WithTestData(session =>
            {
                var result = session.ReadCoverage();
                xml = result.GetOpenCoverXml();
            });

            return Verifier.Verify(xml, VerifySettings());
        }

        [Fact]
        public void ListSessions()
        {
            WithDatabase(() =>
            {
                var controller = NewCoverageController();
                var session1 = controller.NewSession();
                var session2 = controller.NewSession();
                var sessions = controller.ListSessions();
                sessions.Should().Contain(session1.SessionName, session2.SessionName);
                foreach (var session in sessions)
                    CodeCoverage
                        .NewController(ConnectionString, DatabaseName, session)
                        .AttachSession()
                        .StopSession();
            });
        }
    }
}
