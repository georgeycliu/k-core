using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinSampleVariable : GremlinTableVariable
    {
        public int AmountToSample { get; set; }

        public GremlinSampleVariable(int amountToSample)
            : base(GremlinVariableType.Table)
        {
            this.AmountToSample = amountToSample;
        }
    }

    internal class GremlinSampleGlobalVariable : GremlinSampleVariable
    {
        public GremlinToSqlContext ProbabilityContext { get; set; }

        public GremlinSampleGlobalVariable(int amountToSample, GremlinToSqlContext probabilityContext)
            : base(amountToSample)
        {
            this.ProbabilityContext = probabilityContext;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            variableList.AddRange(this.ProbabilityContext.FetchAllVars());
            return variableList;
        }

        internal override List<GremlinVariable> FetchAllTableVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            variableList.AddRange(this.ProbabilityContext.FetchAllTableVars());
            return variableList;
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression> {SqlUtil.GetValueExpr(this.AmountToSample)};
            if (this.ProbabilityContext != null) {
                parameters.Add(SqlUtil.GetScalarSubquery(this.ProbabilityContext.ToSelectQueryBlock()));
            }
            WSchemaObjectFunctionTableReference tableRef =
                SqlUtil.GetFunctionTableReference(GremlinKeyword.func.SampleGlobal, parameters, this.GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }

    internal class GremlinSampleLocalVariable : GremlinSampleVariable
    {
        public GremlinVariable InputVariable { get; set; }

        public GremlinSampleLocalVariable(GremlinVariable inputVariable, int amountToSample)
            : base(amountToSample)
        {
            this.InputVariable = inputVariable;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this, this.InputVariable };
            return variableList;
        }

        internal override List<GremlinVariable> FetchAllTableVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            return variableList;
        }

        internal override void Populate(string property)
        {
            InputVariable.Populate(property);
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>
            {
                this.InputVariable.GetDefaultProjection().ToScalarExpression(),
                SqlUtil.GetValueExpr(this.AmountToSample)
            };

            foreach (string property in this.ProjectedProperties) {
                parameters.Add(SqlUtil.GetValueExpr(property));
            }

            WSchemaObjectFunctionTableReference tableRef =
                SqlUtil.GetFunctionTableReference(GremlinKeyword.func.SampleLocal, parameters, this.GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}
