using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinAddVVariable: GremlinVertexTableVariable
    {
        public List<GremlinProperty> VertexProperties { get; set; }
        public string VertexLabel { get; set; }
        public bool IsFirstTableReference { get; set; }

        internal override void Populate(string property)
        {
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(SqlUtil.GetValueExpr(VertexLabel));
            foreach (var vertexProperty in VertexProperties)
            {
                parameters.Add(vertexProperty.ToPropertyExpr());
            }
            foreach (string property in this.ProjectedProperties) {
                parameters.Add(SqlUtil.GetValueExpr(property));
            }

            var secondTableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.AddV, parameters, GetVariableName());

            var crossApplyTableRef = SqlUtil.GetCrossApplyTableReference(secondTableRef);
            crossApplyTableRef.FirstTableRef = IsFirstTableReference ? SqlUtil.GetDerivedTable(SqlUtil.GetSimpleSelectQueryBlock("1"), "_") : null;
            return crossApplyTableRef;
        }

        public GremlinAddVVariable(string vertexLabel, List<GremlinProperty> vertexProperties, bool isFirstTableReference = false)
        {
            VertexProperties = new List<GremlinProperty>(vertexProperties);
            VertexLabel = vertexLabel;
            IsFirstTableReference = isFirstTableReference;
        }
    }
}
