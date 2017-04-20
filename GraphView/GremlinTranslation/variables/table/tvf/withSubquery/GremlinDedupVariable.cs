using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinDedupVariable : GremlinTableVariable
    {
        public GremlinVariable InputVariable { get; set; }
        public GremlinToSqlContext DedupContext { get; set; }

        public GremlinDedupVariable(GremlinVariable inputVariable, 
                                    GremlinToSqlContext dedupContext) : base(GremlinVariableType.Table)
        {
            this.InputVariable = inputVariable;
            this.DedupContext = dedupContext;
        }

        internal override List<GremlinVariable> FetchAllTableVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            if (this.DedupContext != null)
                variableList.AddRange(this.DedupContext.FetchAllTableVars());
            return variableList;
        }
    }

    internal class GremlinDedupGlobalVariable : GremlinDedupVariable
    {
        public List<GremlinVariable> DedupVariables { get; set; }
        public GremlinDedupGlobalVariable(GremlinVariable inputVariable,
                                    List<GremlinVariable> dedupVariables,
                                    GremlinToSqlContext dedupContext
                                    )
            : base(inputVariable, dedupContext)
        {
            this.DedupVariables = dedupVariables;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable> {this, this.InputVariable};
            variableList.AddRange(this.DedupVariables);
            if (this.DedupContext != null)
                variableList.AddRange(this.DedupContext.FetchAllVars());
            return variableList;
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            if (this.DedupVariables.Count > 0)
            {
                parameters.AddRange(this.DedupVariables.Select(dedupVariable => dedupVariable.GetDefaultProjection().ToScalarExpression()));
            }
            else
            {
                parameters.Add(SqlUtil.GetScalarSubquery(this.DedupContext.ToSelectQueryBlock()));
            }

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.DedupGlobal, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }

    internal class GremlinDedupLocalVariable : GremlinDedupVariable
    {
        public GremlinDedupLocalVariable(GremlinVariable inputVariable,
                                    GremlinToSqlContext dedupContext)
            : base(inputVariable, dedupContext)
        {
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable> {this, this.InputVariable};
            if (this.DedupContext != null)
                variableList.AddRange(this.DedupContext.FetchAllVars());
            return variableList;
        }

        internal override void Populate(string property)
        {
            this.InputVariable?.Populate(property);
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>
            {
                SqlUtil.GetScalarSubquery(this.DedupContext.ToSelectQueryBlock())
            };

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.DedupLocal, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}
